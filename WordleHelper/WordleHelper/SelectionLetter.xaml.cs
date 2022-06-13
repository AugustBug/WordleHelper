using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WordleHelper
{

    public partial class SelectionLetter : UserControl
    {
        private MainWindow parent;
        private MyWordleHelper.RESULT result;
        private int index;

        public SelectionLetter()
        {
            InitializeComponent();
        }

        public void init(MainWindow parent, int index)
        {
            this.parent = parent;
            this.index = index;
            this.result = MyWordleHelper.RESULT.EMPTY;

            txtLetter.TabIndex = index;
            txtLetter.Background = Brushes.White;
        }

        public void reset()
        {
            txtLetter.Text = "";
            result = MyWordleHelper.RESULT.EMPTY;
            txtLetter.Background = Brushes.White;
        }

        public void autoSet(char c)
        {
            txtLetter.Text = "" + c;
            result = MyWordleHelper.RESULT.EMPTY;
            txtLetter.Background = Brushes.White;
        }

        public bool isCompleted()
        {
            return (txtLetter.Text.Length == 1 && result != MyWordleHelper.RESULT.EMPTY);
        }

        public Tuple<char, MyWordleHelper.RESULT> getInfo()
        {
            if (!isCompleted())
            {
                return null;
            }
            return new Tuple<char, MyWordleHelper.RESULT>(txtLetter.Text[0], result);
        }

        private void BtnGray_Click(object sender, RoutedEventArgs e)
        {
            result = MyWordleHelper.RESULT.WRONG;
            txtLetter.Background = Brushes.Gray;
        }

        private void BtnYellow_Click(object sender, RoutedEventArgs e)
        {
            result = MyWordleHelper.RESULT.CORRECT_BUT_WRONG_POS;
            txtLetter.Background = Brushes.Yellow;
        }

        private void BtnGreen_Click(object sender, RoutedEventArgs e)
        {
            result = MyWordleHelper.RESULT.CORRECT;
            txtLetter.Background = Brushes.Green;
        }

        private void TxtLetter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtLetter.Text.Length > 0)
            {
                if (!parent.isValidChar(txtLetter.Text[0]))
                {
                    char val = parent.getValidChar(txtLetter.Text[0]);
                    if (val == '\0')
                    {
                        txtLetter.Text = "";
                    }
                    else
                    {
                        txtLetter.Text = "" + parent.getValidChar(txtLetter.Text[0]);
                    }
                }
            }
        }
    }
}
