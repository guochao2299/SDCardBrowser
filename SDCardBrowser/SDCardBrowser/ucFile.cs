using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SDCardBrowser
{
    public partial class ucFile : UserControl
    {
        public ucFile()
        {
            InitializeComponent();
        }

        public Image Image
        {
            set
            {
                this.picImage.Image = value;
            }
        }

        public override string Text
        {
            get
            {
                return this.lblText.Text ;
            }
            set
            {
                base.Text = value;
                this.lblText.Text = value;
            }
        }

        private bool m_isDir = false;
        public bool IsDir
        {
            get
            {
                return m_isDir;
            }
            set
            {
                m_isDir = value;
            }
        }

        private bool m_back2Parent = false;
        public bool Back2Parent
        {
            get
            {
                return m_back2Parent;
            }
            set
            {
                m_back2Parent = value;
            }
        }

        public event EventHandler DirOpening;

        private void picImage_DoubleClick(object sender, EventArgs e)
        {
            if (IsDir && (DirOpening != null))
            {
                DirOpening(this, EventArgs.Empty);
            }
        }
    }
}
