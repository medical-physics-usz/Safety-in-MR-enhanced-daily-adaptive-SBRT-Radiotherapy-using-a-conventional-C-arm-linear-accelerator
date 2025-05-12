using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace USZ_RtPlanAutomator.QA
{
    class Verificator
    {
        // Creates a portable dose verification plan for an empty image or a phantom image
        public static void CreateVerificationPlan(Patient patient, PlanSetup plan, bool useEmptyImage, bool clinicalVersion = true)
        {
            if (!(plan is ExternalPlanSetup))
            {
                throw new Exception("Verification plan can not be created, not an external beam plan!");
            }

            ExternalPlanSetup eps = (ExternalPlanSetup)plan;

            if (useEmptyImage)
            {
                CreateEmptyVerificationPlan(patient, eps);
            }
            else
            {
                CreateVerificationPlanWithPhantom(patient, eps, clinicalVersion);
            }

        }

        private static void CreateEmptyVerificationPlan(Patient patient, ExternalPlanSetup plan)
        {
            string phantomImageId = "emptyPhantom";

            // Read orientation and image sizes from plan, and convert so, that pixels >= 1 cm in each direction
            var origImage = plan.StructureSet.Image;
            PatientOrientation orientation = origImage.ImagingOrientation;
            int widthInPixels = origImage.XSize;
            int heightInPixels = origImage.YSize;
            double widthMM = origImage.XRes * widthInPixels;
            double heightMM = origImage.YRes * heightInPixels;
            int planesN = origImage.YSize;
            double planeSepMM = origImage.ZRes;

            Course qaCourse = CreateQaCourse(patient);

            var emptyStructureSet = patient.AddEmptyPhantom(phantomImageId, orientation, widthInPixels, heightInPixels, widthMM, heightMM, planesN, planeSepMM);
            string verificationPlanName = getVerificationPlanName(plan.Course.Id, plan.Id, true);

            if(qaCourse.PlanSetups.Any(x => x.Id == verificationPlanName))
            {
                throw new Exception($"Can not create verification plan, plan {verificationPlanName} already exists!");
            }
            ExternalPlanSetup verificationPlan = qaCourse.AddExternalPlanSetupAsVerificationPlan(emptyStructureSet, plan);
            verificationPlan.Id = verificationPlanName;
        }

        private static void CreateVerificationPlanWithPhantom(Patient patient, ExternalPlanSetup plan, bool clinicalVersion = true)
        {
            Course qaCourse = CreateQaCourse(patient);

            string error;
            string phantomPatientId;
            string phantomStudyId;
            string phantomImageId;

            // Phantom details for clinical Aria
            if (clinicalVersion) 
            {
                phantomPatientId = "zzz_delta4";
                phantomStudyId = "S";
                phantomImageId = "CT_with_couch";
            }
            // Phantom details for TBox
            else
            {
                phantomPatientId = "211020";
                phantomStudyId = "";
                phantomImageId = "QA Delta 4 test";
            }

            try
            {
                if (patient.CanCopyImageFromOtherPatient(null, phantomPatientId, phantomStudyId, phantomImageId, out error))
                {
                    var phantomStructureSet = patient.CopyImageFromOtherPatient(phantomPatientId, phantomStudyId, phantomImageId);
                    string verificationPlanName = getVerificationPlanName(plan.Course.Id, plan.Id, false);

                    if (qaCourse.PlanSetups.Any(x => x.Id == verificationPlanName))
                    {
                        throw new Exception($"Can not create verification plan, plan {verificationPlanName} already exists!");
                    }
                    ExternalPlanSetup verificationPlan = qaCourse.AddExternalPlanSetupAsVerificationPlan(phantomStructureSet, plan);
                    verificationPlan.Id = verificationPlanName;
                }
                else
                {
                    throw new Exception(error);
                }
            }
            catch (Exception excp)
            {
                throw new Exception($"Verification plan could not be created: {excp}");
            }
        }

        private static Course CreateQaCourse(Patient patient)
        {
            string QAcourseId = "QA"; // "z_QA"; // use this when testing
            Course course = patient.Courses.Where(x => x.Id == QAcourseId).FirstOrDefault();
            if (course == null)
            {
                course = patient.AddCourse();
                course.Id = QAcourseId;
            }
            return course;
        }
       
        private static string getVerificationPlanName(string courseId, string planId, bool useEmptyImage)
        {
            int planNameMaxLength = 13; // Plan name maximum length is 13 in eclipse
            string verficationPlanName = "";

            string[] courseSubs = courseId.Split('_');
            if(courseSubs.Length>=2)
            {
                verficationPlanName = courseSubs[0] + "_";
            }

            if(useEmptyImage)
            {
                verficationPlanName += "PD_";
            }

            verficationPlanName += planId;

            if(verficationPlanName.Length> planNameMaxLength) 
            {
                verficationPlanName = verficationPlanName.Substring(0, planNameMaxLength);
            }

            return verficationPlanName;
        }
    }
}
