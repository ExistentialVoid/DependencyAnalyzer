using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Reflection;
using DependencyAnalyzer;

namespace SDILReaderTest
{
    public partial class frmTestILReader : Form
    {
        public List<MethodInfo> Methods = new List<MethodInfo>();

        public frmTestILReader()
        {
            Architect.LoadOpCodes();
            InitializeComponent();
        }

        private void btnOpenAssembly_Click(object sender, EventArgs e)
        {
            // clear the methods cache
            Methods.Clear();

            // clear the listview with the available methods
            LstAvailableMethodsList.Items.Clear();

            dlgOpenAssembly.ShowDialog();
            // get the filename of the assembly
            string assemblyName = dlgOpenAssembly.FileName;
            try
            {
                // load the assembly
                Assembly A = Assembly.LoadFile(assemblyName);

                // get all the methods within the loaded assembly
                A.GetModules().ToList().ForEach(mod =>
                {
                    mod.GetTypes().ToList().ForEach(t =>
                    {
                        BindingFlags bfs = BindingFlags.DeclaredOnly | BindingFlags.Public 
                        | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
                        t.GetMethods(bfs).ToList().ForEach(m =>
                        {
                            // check if the method has a body
                            if (m.GetMethodBody() != null)
                            {
                                Methods.Add(m);
                                LstAvailableMethodsList.Items.Add(m.Name);
                            }
                        });
                    });
                });
            }
            catch { MessageBox.Show("Invalid assembly"); }
        }

        private void LstAvailableMethodsList_SelectedValueChanged(object sender, EventArgs e)
        {
            try
            {
                MethodInfo method = Methods[LstAvailableMethodsList.SelectedIndex];
                DependencyAnalyzer.MethodBodyReader mr = new MethodBodyReader(method);
                rchMethodBodyCode.Clear();
                rchMethodBodyCode.Text = mr.GetBodyCode();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}");
            }
        }
    }
}