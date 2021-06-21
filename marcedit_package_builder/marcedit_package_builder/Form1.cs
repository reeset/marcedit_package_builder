using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Resources;
using System.IO;
using System.Security.Cryptography;
using System.CodeDom.Compiler;
using System.Diagnostics;
using Microsoft.CSharp;

namespace marcedit_package_builder
{
    public partial class frmMain : Form
    {
        private string pmysalt = "s@lt_th1$_$tr1ng_M@rc"; 
        public frmMain()
        {
            InitializeComponent();
            //AppDomain.CurrentDomain.AssemblyResolve += OnCurrentDomainAssemblyResolve;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.OpenFileDialog od = new OpenFileDialog();
            od.AddExtension = true;
            od.DefaultExt = ".exe";
            od.Filter = "Executable File (*.exe)|*.exe|Microsoft Installer File (*.msi)|*.msi|All Files (*.*)|*.*";
            od.FilterIndex = 0;
            od.ShowDialog();
            txtMSI.Text = od.FileName;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.SaveFileDialog sd = new SaveFileDialog();
            sd.AddExtension = true;
            sd.DefaultExt = ".exe";
            sd.Filter = "Executable File (*.exe)|*.exe";
            sd.FilterIndex = 0;
            sd.OverwritePrompt = true;
            sd.ShowDialog();
            txtSave.Text = sd.FileName;
        }

        private void cmdBuild_Click(object sender, EventArgs e)
        {
            byte[] file_bytes = null;
            System.IO.FileStream objF = new System.IO.FileStream(txtMSI.Text, System.IO.FileMode.Open, System.IO.FileAccess.Read); 
            file_bytes = new byte[objF.Length];
            objF.Read(file_bytes, 0, (int)objF.Length);

            objF.Close();
            string mysalt = pmysalt;

            if (txt_Salt.Text.Length > 0)
            {
                mysalt = txt_Salt.Text;
            }

            System.Resources.ResourceWriter res = new ResourceWriter(@".\build_resources.res");
            res.AddResourceData("file_type", "string", System.Text.Encoding.GetEncoding(1252).GetBytes(System.IO.Path.GetExtension(txtMSI.Text)));
            res.AddResourceData("salt", "string", System.Text.Encoding.GetEncoding(1252).GetBytes(mysalt));
            res.AddResourceData("Username", "string", System.Text.Encoding.GetEncoding(1252).GetBytes(EncryptText(txtUsername.Text, mysalt)));
            res.AddResourceData("Password", "string", System.Text.Encoding.GetEncoding(1252).GetBytes(EncryptText(txtPassword.Text, mysalt)));
            if (txt_Domain.Text.Trim().Length > 0)
            {
                res.AddResourceData("Domain", "string", System.Text.Encoding.GetEncoding(1252).GetBytes(EncryptText(txt_Domain.Text, mysalt)));
            }
            else
            {
                res.AddResourceData("Domain", "string", System.Text.Encoding.GetEncoding(1252).GetBytes(EncryptText("{no domain defined}", mysalt)));
            }
            res.AddResourceData("test_file", "byte[]", file_bytes);
            res.Close();


            CSharpCodeProvider codeProvider = new CSharpCodeProvider(new Dictionary<String, String> { { "CompilerVersion", "v4.0" } });
            
            //ICodeCompiler icc = codeProvider.CreateCompiler();
            //icc = codeProvider.c
            
            System.CodeDom.Compiler.CompilerParameters parameters = new CompilerParameters();
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            parameters.ReferencedAssemblies.Add("System.Core.dll");
            parameters.ReferencedAssemblies.Add("Microsoft.CSharp.dll");
            parameters.ReferencedAssemblies.Add("System.Security.dll");

            //Make sure we generate an EXE, not a DLL
            parameters.GenerateExecutable = true;
            parameters.EmbeddedResources.Add(@".\build_resources.res");
            parameters.OutputAssembly = txtSave.Text;
            //CompilerResults results = icc.CompileAssemblyFromSource(parameters, textBox1.Text);
            CompilerResults results = codeProvider.CompileAssemblyFromFile(parameters, new string[] { @".\Program.cs" });
            if (results.Errors.Count > 0)
            {
                string err_text = "";
                foreach (CompilerError CompErr in results.Errors)
                {
                    err_text += "Line number " + CompErr.Line +
                                ", Error Number: " + CompErr.ErrorNumber +
                                ", '" + CompErr.ErrorText + ";" +
                                Environment.NewLine + Environment.NewLine;
                }
                System.Windows.Forms.MessageBox.Show(err_text);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Compiler Completed");
            }
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Encrypt/decrypt code from: http://www.codeproject.com/Articles/769741/Csharp-AES-bits-Encryption-Library-with-Salt
        /// </summary>
        /// <param name="bytesToBeEncrypted"></param>
        /// <param name="passwordBytes"></param>
        /// <returns></returns>
        private byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
        {
            byte[] encryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            byte[] saltBytes = new byte[] { 0, 4, 2, 9, 2, 1, 6, 6 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            return encryptedBytes;
        }

        private string EncryptText(string input, string password)
        {
            // Get the bytes of the string
            byte[] bytesToBeEncrypted = System.Text.Encoding.UTF8.GetBytes(input);
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);

            // Hash the password with SHA256
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            byte[] bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, passwordBytes);

            string result = Convert.ToBase64String(bytesEncrypted);

            return result;
        }
        
        private  System.Reflection.Assembly OnCurrentDomainAssemblyResolve(object sender, ResolveEventArgs args)
        {
            // this is absurdly expensive...don't do this more than once, or load the assembly file in a more efficient way
            // also, if the code you're using to compile the CodeDom assembly doesn't/hasn't used the referenced assembly yet, this won't work
            // and you should use Assembly.Load(...)
            foreach (System.Reflection.Assembly @assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (@assembly.FullName.Equals(args.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return @assembly;
                }
            }
            return null;
        }
    }
}
