using System;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.InteropServices;


namespace secure_shell
{
    static class Program
    {

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetShortPathName(
                 [MarshalAs(UnmanagedType.LPTStr)]
                   string path,
                 [MarshalAs(UnmanagedType.LPTStr)]
                   System.Text.StringBuilder shortPath,
                 int shortPathLength
                 );

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            //Extract the Resource file
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string[] list = assembly.GetManifestResourceNames();


            System.IO.Stream stream = assembly.GetManifestResourceStream("build_resources.res");

            using (System.IO.FileStream fileStream = new System.IO.FileStream(@".\build_resources.res", System.IO.FileMode.Create))
            {
                for (int i = 0; i < stream.Length; i++)
                {
                    fileStream.WriteByte((byte)stream.ReadByte());
                }
                fileStream.Close();
            }

            string resType = "";
            byte[] data = null;
            System.Resources.ResourceReader res = new System.Resources.ResourceReader(@".\build_resources.res");

            string mysalt = "s@lt_th1$_$tr1ng_M@rc";

            res.GetResourceData("salt", out resType, out data);
            if (data != null && data.Length !=0)
            {
                mysalt = System.Text.Encoding.GetEncoding(1252).GetString(data);
            }


            res.GetResourceData("file_type", out resType, out data);
            string sfile_type = System.Text.Encoding.GetEncoding(1252).GetString(data);

            Console.WriteLine("file_type: " + sfile_type);

            res.GetResourceData("Username", out resType, out data);
            //System.Windows.Forms.MessageBox.Show(DecryptText(System.Text.Encoding.GetEncoding(1252).GetString(data), "s@lt_th1$_$tr1ng_M@rc"));

            string sUsername = DecryptText(System.Text.Encoding.GetEncoding(1252).GetString(data), mysalt);

            res.GetResourceData("Password", out resType, out data);
            string sPassword = DecryptText(System.Text.Encoding.GetEncoding(1252).GetString(data), mysalt);

            string sDomain = "";
            res.GetResourceData("Domain", out resType, out data);
            if (data != null || data.Length > 0)
            {
                sDomain = DecryptText(System.Text.Encoding.GetEncoding(1252).GetString(data), mysalt);
                if (sDomain == "{no domain defined}")
                {
                    sDomain = String.Empty;
                }
            }

            res.GetResourceData("test_file", out resType, out data);

            string tmpfilename = "";
            switch (sfile_type.ToLower())
            {
                case ".exe":
                    tmpfilename = System.Guid.NewGuid().ToString("N") + ".exe";
                    break;
                default:
                    tmpfilename = System.Guid.NewGuid().ToString("N") + ".msi";
                    break;
            }
            //tmpfilename = System.Guid.NewGuid().ToString("N") + ".msi";
            System.IO.FileStream objS = new System.IO.FileStream(@".\" + tmpfilename, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            objS.Write(data, 0, data.Length);
            objS.Close();

            System.Security.SecureString securePwd = new System.Security.SecureString();
            foreach (char c in sPassword)
            {
                securePwd.AppendChar(c);
            }


            //const int ERROR_CANCELLED = 1223; //The operation was canceled by the user.
            string filename = System.IO.Path.GetDirectoryName(assembly.Location) + System.IO.Path.DirectorySeparatorChar.ToString() + tmpfilename;
            System.Text.StringBuilder shortPath = new System.Text.StringBuilder(255);

            //System.Windows.Forms.MessageBox.Show("Username: " + sUsername + "\n" +
            //                                     "Password: " + sPassword + "\n" +
            //                                     "Domain: " + sDomain);
            
            GetShortPathName(filename, shortPath, shortPath.Capacity);
            //System.Windows.Forms.MessageBox.Show(filename);
            /*System.Windows.Forms.MessageBox.Show("File Type: " + sfile_type + "\n" +
                "Filename: " + filename + "\n" +
                "short name: " + shortPath + "\n" +
                "domain: " + sDomain + "\n" +
                "username: " + sUsername + "\n" +
                "password: " + sPassword);
            */
            System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo();
            switch (sfile_type.ToLower())
            {
                case ".exe":
                    info.FileName = filename;
                    info.Arguments = "/exenoui /qn";
                    info.UseShellExecute = false;
                    info.Domain = sDomain;
                    info.UserName = sUsername;
                    info.Password = securePwd;
                    break;
                default:

                    info.FileName = "msiexec";
                    info.Arguments = "/i " + shortPath.ToString();
                    info.UseShellExecute = false;
                    info.Domain = sDomain;
                    info.UserName = sUsername;
                    info.Password = securePwd;
                    //System.Diagnostics.Process.Start(info);
                    break;
            }
            //info.Verb = "runas";
            try
            {

                //System.Windows.Forms.MessageBox.Show(info.FileName);
                while (System.IO.File.Exists(filename) == false)
                {
                    System.Threading.Thread.Sleep(500);
                }
                System.Diagnostics.Process.Start(info);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.ToString());
            }

        }


        private static byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            byte[] decryptedBytes = null;

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

                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }

            return decryptedBytes;
        }
        
        private static string DecryptText(string input, string password)
        {
            // Get the bytes of the string
            byte[] bytesToBeDecrypted = Convert.FromBase64String(input);
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            byte[] bytesDecrypted = AES_Decrypt(bytesToBeDecrypted, passwordBytes);

            string result = System.Text.Encoding.UTF8.GetString(bytesDecrypted);

            return result;
        }
    }
}
