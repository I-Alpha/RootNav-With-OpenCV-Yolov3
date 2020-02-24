using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RootNav.Core.Measurement
{
    public abstract class PlantComponent : BindablePlantComponent
    {

        private int id = 0;

        public int ID
        {
            get { return id; }
            set { id = value; }
        }

        public string RelativeID
        {
            get
            {
                string current = this.ID.ToString();

                PlantComponent parent = this.parent;
                while (parent != null)
                {
                    current = current.Insert(0, parent.ID.ToString() + ".");
                    parent = parent.Parent;
                }
                return current;
            }
        }

        private PlantComponent parent = null;

        public PlantComponent Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        public abstract int Order
        {
            get;
        }

        private List<PlantComponent> children = new List<PlantComponent>();

        public List<PlantComponent> Children
        {
            get { return children; }
            set { children = value; }
        }

        private string label = "";

        public string Label
        {
            get 
            {
                return label;
            }
            set
            {
                label = value;
                base.RaisePropertyChanged("Label");
            }
        }
    }
}
