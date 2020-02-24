using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RootNav.Core.DataStructures
{
    class Shape
    {
        public int width;
        public int height;

        public Shape()
        {
            width = 0;
            height = 0;
        }

        public Shape(int w, int h)
        {
            this.width = w;
            this.height = h;
        }

        public virtual void Render()
        {

        }

    }


    class Square : Shape
    {
        public Square(int w, int h)
            : base(w, h)
        {

        }

        public override void Render()
        {
            // Some code to draw a square
            base.Render();
        }

    }

    class Circle : Shape
    {
        public Circle(int w, int h)
            : base(w, h)
        {
            
        }

        public override void Render()
        {
            // Some code to draw a circle
            base.Render();
        }

    }

}
