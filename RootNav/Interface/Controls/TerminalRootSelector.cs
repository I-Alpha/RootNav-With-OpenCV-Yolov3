using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

using RootNav.Core.LiveWires;

namespace RootNav.Interface.Controls
{
    public partial class TerminalRootSelector : Control
    {
        public delegate void TerminalNodeSelectedHandler(object sender, TerminalType type, int rootIndex);

        public event EventHandler Closed;
        public event TerminalNodeSelectedHandler TerminalNodeSelected;

        private bool isClosing = false;

        public bool IsClosing
        {
            get { return isClosing; }
            set { isClosing = value; }
        }

        static TerminalRootSelector()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TerminalRootSelector), new FrameworkPropertyMetadata(typeof(TerminalRootSelector)));
        }

        private RelativePositionPanel mainPanel;

        private int terminalIndex = -1;

        public int TerminalIndex
        {
            get { return terminalIndex; }
        }

        private ControlAdorner adornerParent = null;

        internal ControlAdorner ControlAdornerParent
        {
            get { return adornerParent; }
        }

        private TerminalType type = TerminalType.Undefined;

        public TerminalType Type
        {
            get { return type; }
        }
       
        private List<int> associatedRootIndexes = null;

        private ScreenOverlayRenderInfo renderInfo = null;

        public void Initialize(int terminalIndex, TerminalType type, List<int> rootIndexes, ScreenOverlayRenderInfo renderInfo)
        {
            this.terminalIndex = terminalIndex;
            this.type = type;
            this.associatedRootIndexes = rootIndexes;
            this.renderInfo = renderInfo;
        }

        public TerminalRootSelector(ControlAdorner parent)
        {
            // Force creation of template before object is added into the visual tree, add an event handler to loaded to create the required UI elements
            BeginInit();

            ApplyTemplate();

            this.Loaded += new RoutedEventHandler(TerminalRootSelector_Loaded);

            this.adornerParent = parent;

            EndInit();
        }

        void TerminalRootSelector_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.terminalIndex < 0 || this.associatedRootIndexes == null || this.renderInfo == null)
            {
                throw new ArgumentException("Cannot create a root selector using these variables");
            }

            this.mainPanel = this.Template.FindName("mainPanel", this) as RelativePositionPanel;

            if (this.mainPanel == null)
            {
                throw new ArgumentNullException("Cannot find the template item mainPanel");
            }

            Point start = new Point(0.5, 0);
            double rotation = 0;
            double rotationStep = 360.0 / this.associatedRootIndexes.Count;

            foreach (int i in this.associatedRootIndexes)
            {
                RotateTransform rotateTransform = new RotateTransform(rotation, 0.5, 0.5);
                Point rotatedPoint = rotateTransform.Transform(start);

                SolidColorBrush backgroundBrush = new SolidColorBrush(renderInfo.HighlightedRootColors[i]);
                if (backgroundBrush.CanFreeze)
                {
                    backgroundBrush.Freeze();
                }

                TerminalNode tn = new TerminalNode() { Background = backgroundBrush, RootIndex = i, Cursor = Cursors.Hand };
                tn.MouseDown += new MouseButtonEventHandler(TerminalNodeMouseDown);
                RelativePositionPanel.SetRelativePositionX(tn, rotatedPoint.X);
                RelativePositionPanel.SetRelativePositionY(tn, rotatedPoint.Y);
                this.mainPanel.Children.Add(tn);

                rotation += rotationStep;
            }

            this.MouseLeftButtonDown += new MouseButtonEventHandler(TerminalRootSelector_MouseLeftButtonDown);
            this.MouseRightButtonDown += new MouseButtonEventHandler(TerminalRootSelector_MouseRightButtonDown);
        }

        void TerminalRootSelector_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;
        }

        void TerminalRootSelector_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;
        }

        void TerminalNodeMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!this.isClosing)
            {
                TerminalNode tn = sender as TerminalNode;
                if (tn != null)
                {
                    if (this.TerminalNodeSelected != null)
                    {
                        tn.Visibility = System.Windows.Visibility.Hidden;
                        this.TerminalNodeSelected(this, this.Type, tn.RootIndex);
                    }
                }
            }
        }

        public void Close()
        {
            this.isClosing = true;

            DoubleAnimation closeAnimation = new DoubleAnimation();
            closeAnimation.From = 1;
            closeAnimation.To = 0;
            closeAnimation.Duration = new Duration(TimeSpan.Parse("0:0:0.150"));
            closeAnimation.Completed += new EventHandler(closeAnimation_Completed);

            DoubleAnimation closeAnimation2 = new DoubleAnimation();
            closeAnimation2.From = 1;
            closeAnimation2.To = 0;
            closeAnimation2.Duration = new Duration(TimeSpan.Parse("0:0:0.150"));

            ScaleTransform scale = this.mainPanel.RenderTransform as ScaleTransform;

            if (scale != null)
            {
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, closeAnimation);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, closeAnimation2);
            }
            else
            {
                BeginAnimation(TerminalRootSelector.OpacityProperty, closeAnimation);
            }
        }

        void closeAnimation_Completed(object sender, EventArgs e)
        {
            if (this.Closed != null)
                this.Closed(this, null);
        }
    }

    public class TerminalNode : Control
    {
        static TerminalNode()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TerminalNode), new FrameworkPropertyMetadata(typeof(TerminalNode)));
        }

        private int rootIndex = -1;

        public int RootIndex
        {
            get { return rootIndex; }
            set { rootIndex = value; }
        }

    }
   
}
