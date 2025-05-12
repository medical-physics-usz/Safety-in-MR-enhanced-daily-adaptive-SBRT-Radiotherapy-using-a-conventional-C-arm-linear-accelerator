using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USZ_RtPlanAutomator.Actions
{
    class ActionTools
    {
        public static string Id2TwentyChar(string Id_in)
        {
            // Fill empty spaces until reaching 20 characters

            string temp_ID = Id_in;

            for (int i = 0; i < 25; i++)
            {
                if (temp_ID.Length >= 20)
                {
                    break;
                }
                else
                {
                    temp_ID = temp_ID + " ";
                }

            }

            return temp_ID;

        }


        public static string Image2ProposedId(VMS.TPS.Common.Model.API.Image Image_in)
        {
            // Build the proposed ID from the image

            string outProposedId = "";

            // Get the date
            string Image_in_Date = DateTime2YYMMDD(Image_in.CreationDateTime.ToString());

            if (Image_in.Series.Modality.ToString().Contains("CT"))
            {

                if (Image_in.Series.Comment.Contains("%"))
                {
                    // 4D CT
                    outProposedId = CT2Id_prefix(Image_in);

                }
                else
                {
                    // 3D CT
                    outProposedId = CT2Id_prefix(Image_in) + "_" + Image_in_Date;
                }


            }
            else if (Image_in.Series.Modality.ToString().Contains("MR"))
            {
                outProposedId = MR2Id_prefix(Image_in) + "_" + Image_in_Date + MR2Id_sufffix(Image_in);

            }
            else
            {
                outProposedId = Image_in.Series.Modality.ToString();

            }
            return outProposedId;
        }


        public static string DateTime2YYMMDD(string inDateTime)
        {
            string outDate = "";

            if (inDateTime.Length >= 10)
            {
                outDate = inDateTime.Substring(8, 2) + "-" + inDateTime.Substring(3, 2) + "-" + inDateTime.Substring(0, 2);
            }

            return outDate;
        }


        public static string CT2Id_prefix(VMS.TPS.Common.Model.API.Image Image_in)
        {
            string outPrefix = "";

            string tempDescription = Image_in.Series.Comment;

            // dictionary for the CT prefix
            if (tempDescription.Contains("SyntheticCT")) { outPrefix = "sCT"; }
            else if (tempDescription.Contains("10%")) { outPrefix = "CT_10"; }
            else if (tempDescription.Contains("20%")) { outPrefix = "CT_20"; }
            else if (tempDescription.Contains("30%")) { outPrefix = "CT_30"; }
            else if (tempDescription.Contains("40%")) { outPrefix = "CT_40"; }
            else if (tempDescription.Contains("50%")) { outPrefix = "CT_50"; }
            else if (tempDescription.Contains("60%")) { outPrefix = "CT_60"; }
            else if (tempDescription.Contains("70%")) { outPrefix = "CT_70"; }
            else if (tempDescription.Contains("80%")) { outPrefix = "CT_80"; }
            else if (tempDescription.Contains("90%")) { outPrefix = "CT_90"; }
            else if (tempDescription.Contains("100%")) { outPrefix = "CT_100"; }
            else { outPrefix = "CT"; } // all the rest

            return outPrefix;
        }



        public static string MR2Id_prefix(VMS.TPS.Common.Model.API.Image Image_in)
        {
            string outPrefix = "";

            string tempDescription = Image_in.Series.Comment;

            // dictionary for the MR prefix
            if (tempDescription.Contains("sCT")) { outPrefix = "DX"; }
            else if (tempDescription.Contains("t1_mprage")) { outPrefix = "T1M"; }
            else if (tempDescription.Contains("t1_space")) { outPrefix = "T1S"; }
            else if (tempDescription.Contains("t1")) { outPrefix = "T1"; }
            else if (tempDescription.Contains("t2")) { outPrefix = "T2"; }
            else if (tempDescription.Contains("flair")) { outPrefix = "Flair"; }
            else if (tempDescription.Contains("diff")) { outPrefix = "DWI"; }
            else if (tempDescription.Contains("tfi")) { outPrefix = "TF"; }
            else if (tempDescription.Contains("MRSIM")) { outPrefix = "TF"; }
            else { outPrefix = "MR"; } // if not recognized

            //add km if km
            if (tempDescription.Contains("pkm")) { outPrefix = outPrefix + "km"; }

            return outPrefix;
        }

        public static string MR2Id_sufffix(VMS.TPS.Common.Model.API.Image Image_in)
        {
            string outSuffix = "";

            string tempDescription = Image_in.Series.Comment;

            // dictionary for the MR suffix
            if (tempDescription.Contains("tra")) { outSuffix = "t"; }
            else if (tempDescription.Contains("cor")) { outSuffix = "c"; }
            else if (tempDescription.Contains("sag")) { outSuffix = "s"; }
            else if (tempDescription.Contains("LargeFOV")) { outSuffix = "F"; }
            else { outSuffix = ""; } // if not recognized

            return outSuffix;
        }
    }
}
