using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.IO;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Diagnostics;

namespace VMS.TPS
{
	/// <summary>
    /// Class script (default).
	/// Author: R. Dal Bello / 24.09.2021
    /// </summary>
    public class Script
    {
        public Script()
        {
        }


        public void Execute(ScriptContext context)
        {
			// Get patient id.
            Patient patient = context.Patient;
			
            if (patient == null)
            {
                MessageBox.Show("Please select a patient");

            }
            else
            {
				
				// a patient was selected, copy the exe for resampling in the appropriate folder and run it
				string sourceFilePath = @"\\raoariaapps\raoariaapps$\Utilities\Resample_CT_SolaAdaption\resample_CT.exe";
				string destinationFilePath = @"\\ariaimg.usz.ch\dicom$\Import\CT\" + patient.Id + @"\resample_CT.exe";
				
				bool exe_copied = false;
				
				try
				{
					// Copy the file to the destination
					File.Copy(sourceFilePath, destinationFilePath, true); // 'true' to overwrite if the file exists
					exe_copied = true;
					//Console.WriteLine("File copied successfully.");
				}
				catch (IOException ioEx)
				{
					Console.WriteLine("An I/O error occurred: " + ioEx.Message);
				}
				catch (UnauthorizedAccessException uaEx)
				{
					Console.WriteLine("Access denied: " + uaEx.Message);
				}
				catch (Exception ex)
				{
					Console.WriteLine("An error occurred: " + ex.Message);
				}
				
				// run the script for sCT resampling
				if (exe_copied)
				{
					Process proc = new Process();
					proc.StartInfo.FileName = destinationFilePath;
					proc.StartInfo.WorkingDirectory = @"\\ariaimg.usz.ch\dicom$\Import\CT\" + patient.Id;
					proc.Start();
				}
				
				// delete the temporary exe
				try
				{
					// Check if the file exists before attempting to delete
					if (File.Exists(destinationFilePath))
					{
						// Delete the file
						File.Delete(destinationFilePath);
						//Console.WriteLine("File deleted successfully.");
					}
					else
					{
						Console.WriteLine("File does not exist.");
					}
				}
				catch (IOException ioEx)
				{
					Console.WriteLine("An I/O error occurred: " + ioEx.Message);
				}
				catch (UnauthorizedAccessException uaEx)
				{
					Console.WriteLine("Access denied: " + uaEx.Message);
				}
				catch (Exception ex)
				{
					Console.WriteLine("An error occurred: " + ex.Message);
				}
            }
        }
    }
}