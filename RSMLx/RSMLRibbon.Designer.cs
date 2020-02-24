namespace RSMLx
{
    partial class RSMLRibbon : Microsoft.Office.Tools.Ribbon.RibbonBase
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        public RSMLRibbon()
            : base(Globals.Factory.GetRibbonFactory())
        {
            InitializeComponent();
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RSMLRibbon));
            this.rsmlTab = this.Factory.CreateRibbonTab();
            this.openGroup = this.Factory.CreateRibbonGroup();
            this.openFileButton = this.Factory.CreateRibbonButton();
            this.openFileDirectory = this.Factory.CreateRibbonButton();
            this.openRSMLFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.rsmlFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.rsmlTab.SuspendLayout();
            this.openGroup.SuspendLayout();
            // 
            // rsmlTab
            // 
            this.rsmlTab.Groups.Add(this.openGroup);
            this.rsmlTab.Label = "RSML";
            this.rsmlTab.Name = "rsmlTab";
            // 
            // openGroup
            // 
            this.openGroup.Items.Add(this.openFileButton);
            this.openGroup.Items.Add(this.openFileDirectory);
            this.openGroup.Label = "RSML";
            this.openGroup.Name = "openGroup";
            // 
            // openFileButton
            // 
            this.openFileButton.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.openFileButton.Image = ((System.Drawing.Image)(resources.GetObject("openFileButton.Image")));
            this.openFileButton.Label = "Open RSML File";
            this.openFileButton.Name = "openFileButton";
            this.openFileButton.ShowImage = true;
            this.openFileButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.openFile_Click);
            // 
            // openFileDirectory
            // 
            this.openFileDirectory.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.openFileDirectory.Image = ((System.Drawing.Image)(resources.GetObject("openFileDirectory.Image")));
            this.openFileDirectory.Label = "Open RSML Directory";
            this.openFileDirectory.Name = "openFileDirectory";
            this.openFileDirectory.ShowImage = true;
            this.openFileDirectory.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.openDirectory_Click);
            // 
            // openRSMLFileDialog
            // 
            this.openRSMLFileDialog.FileName = "openRSMLFileDialog";
            this.openRSMLFileDialog.Filter = "RSML Files|*.rsml|All Files|*.*";
            // 
            // RSMLRibbon
            // 
            this.Name = "RSMLRibbon";
            this.RibbonType = "Microsoft.Excel.Workbook";
            this.Tabs.Add(this.rsmlTab);
            this.Load += new Microsoft.Office.Tools.Ribbon.RibbonUIEventHandler(this.RSMLRibbon_Load);
            this.rsmlTab.ResumeLayout(false);
            this.rsmlTab.PerformLayout();
            this.openGroup.ResumeLayout(false);
            this.openGroup.PerformLayout();

        }

        #endregion

        private Microsoft.Office.Tools.Ribbon.RibbonTab rsmlTab;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup openGroup;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton openFileButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton openFileDirectory;
        private System.Windows.Forms.OpenFileDialog openRSMLFileDialog;
        private System.Windows.Forms.FolderBrowserDialog rsmlFolderBrowserDialog;
    }

    partial class ThisRibbonCollection
    {
        internal RSMLRibbon RSMLRibbon
        {
            get { return this.GetRibbon<RSMLRibbon>(); }
        }
    }
}
