using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using USZ_RtPlanAutomator.DataExtraction;
using USZ_RtPlanAutomator.Optimization;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace USZ_RtPlanAutomator.UserControls
{
    /// <summary>
    /// Interaktionslogik für SelectPtvItv.xaml
    /// </summary>
    /// 
    public class CheckedListItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsChecked { get; set; }
        public bool IsEnabled { get; set; }
    }
    public partial class SelectPtvItv : UserControl
    {
        public SelectPtvItv()
        {
            InitializeComponent();
        }

        public List<Structure> GetSelectedPtvs(PlanSetup plan)
        {
            List<string> selectedPtvIds = GetSelectedPtvIds();
            if (selectedPtvIds == null || selectedPtvIds.Count == 0)
            {
                return null;
            }
            List<Structure> selectedStructures = new List<Structure>();
            foreach (string ptvId in selectedPtvIds)
            {
                Structure ptvStructure = plan.StructureSet.Structures.FirstOrDefault(s => s.Id == ptvId);
                selectedStructures.Add(ptvStructure);
            }
            return selectedStructures;
        }

        public List<Structure> GetSelectedItvs(PlanSetup plan)
        {
            List<string> selectedItvIds = GetSelectedItvIds();
            if (selectedItvIds == null || selectedItvIds.Count == 0)
            {
                return null;
            }
            List<Structure> selectedStructures = new List<Structure>();
            foreach (string itvId in selectedItvIds)
            {
                Structure itvStructure = plan.StructureSet.Structures.FirstOrDefault(s => s.Id == itvId);
                selectedStructures.Add(itvStructure);
            }
            return selectedStructures;
        }

        public List<Structure> GetSelectedOARs(PlanSetup plan)
        {
            List<string> selectedOARIds = GetSelectedOARIds();
            if (selectedOARIds == null || selectedOARIds.Count == 0)
            {
                return null;
            }
            List<Structure> selectedStructures = new List<Structure>();
            foreach (string oarId in selectedOARIds)
            {
                Structure oarStructure = plan.StructureSet.Structures.FirstOrDefault(s => s.Id == oarId);
                selectedStructures.Add(oarStructure);
            }
            return selectedStructures;
        }
        private List<string> GetSelectedPtvIds()
        {
            List<string> selectedPtvs = new List<string>();
            foreach (CheckedListItem ptv in cmbCheckPtv.Items)
            {
                if (ptv.IsChecked)
                {
                    selectedPtvs.Add(ptv.Name);
                }
            }
            return selectedPtvs;
        }

        private List<string> GetSelectedItvIds()
        {
            List<string> selectedItvs = new List<string>();
            foreach (CheckedListItem itv in cmbCheckItv.Items)
            {
                if (itv.IsChecked)
                {
                    selectedItvs.Add(itv.Name);
                }
            }
            return selectedItvs;
        }

        private List<string> GetSelectedOARIds()
        {
            List<string> selectedOARs = new List<string>();
            foreach (CheckedListItem oar in cmbCheckOar.Items)
            {
                if (oar.IsChecked)
                {
                    selectedOARs.Add(oar.Name);
                }
            }
            return selectedOARs;
        }

        public void FillPtvComboBox(PlanSetup plan)
        {
            string ptvType = "PTV";
            cmbCheckPtv.Items.Clear();

            if (plan == null)
            {
                return;
            }

            if (plan != null && plan.StructureSet != null && plan.StructureSet.Structures.Any(s => s.DicomType.Equals(ptvType)))
            {
                var allPtvs = plan.StructureSet.Structures.Where(s => s.DicomType.Equals(ptvType)).ToList();
                allPtvs = allPtvs.OrderBy(x => x.Id).ToList();

                int i = 0;
                foreach (var ptv in allPtvs)
                {
                    CheckedListItem myItem = new CheckedListItem();
                    myItem.Id = i;
                    myItem.Name = ptv.Id;
                    myItem.IsEnabled = true;
                    myItem.IsChecked = myItem.Name == plan.TargetVolumeID ? true : false;
                    cmbCheckPtv.Items.Add(myItem);
                    i++;
                }
            }
            if (cmbCheckPtv.Items.Count==0)
            {
                CheckedListItem myItem = new CheckedListItem();
                myItem.Id = 0;
                myItem.Name = "No PTVs found";
                myItem.IsChecked = false;
                myItem.IsEnabled = false;
                cmbCheckPtv.Items.Add(myItem);
            }
        }

        public void FillItvComboBox(PlanSetup plan)
        {
            string itvType = "CTV";
            string gtvType = "GTV";
            cmbCheckItv.Items.Clear();
            
            if (plan == null)
            {
                return;
            }

            // fill type CTV

            if (plan != null && plan.StructureSet != null && plan.StructureSet.Structures.Any(s => s.DicomType.Equals(itvType)))
            {
                var allItvs = plan.StructureSet.Structures.Where(s => s.DicomType.Equals(itvType)).ToList();
                allItvs = allItvs.OrderBy(x => x.Id).ToList();

                int i = 0;
                foreach (var itv in allItvs)
                {
                    CheckedListItem myItem = new CheckedListItem();
                    myItem.Id = i;
                    myItem.Name = itv.Id;
                    myItem.IsEnabled = true;
                    myItem.IsChecked = myItem.Name == DataExtractor.FindTargetItvId(plan) ? true : false;
                    cmbCheckItv.Items.Add(myItem);
                    i++;
                }
            }

            // fill type GTV

            if (plan != null && plan.StructureSet != null && plan.StructureSet.Structures.Any(s => s.DicomType.Equals(gtvType)))
            {
                var allItvs = plan.StructureSet.Structures.Where(s => s.DicomType.Equals(gtvType)).ToList();
                allItvs = allItvs.OrderBy(x => x.Id).ToList();

                int i = 0;
                foreach (var itv in allItvs)
                {
                    CheckedListItem myItem = new CheckedListItem();
                    myItem.Id = i;
                    myItem.Name = itv.Id;
                    myItem.IsEnabled = true;
                    myItem.IsChecked = myItem.Name == DataExtractor.FindTargetItvId(plan) ? true : false;
                    cmbCheckItv.Items.Add(myItem);
                    i++;
                }
            }

            // check if empty
            if (cmbCheckItv.Items.Count == 0)
            {
                CheckedListItem myItem = new CheckedListItem();
                myItem.Id = 0;
                myItem.Name = "No GTV/CTV/ITVs found";
                myItem.IsChecked = false;
                myItem.IsEnabled = false;
                cmbCheckItv.Items.Add(myItem);
            }
        }

        public void FillOarComboBox(PlanSetup plan)

        {
            //string oarType = "Organ";
            cmbCheckOar.Items.Clear();

            if (plan == null)
            {
                return;
            }

            // fill type Organ
            if (plan.StructureSet != null)
            {
                List<Structure> allOars = new List<Structure>();
                foreach (Structure str in plan.StructureSet.Structures)
                {
                    if (!str.DicomType.Contains("TV"))
                    {
                        allOars.Add(str);
                    }
                }
                allOars = allOars.OrderBy(x => x.Id).ToList();

                int i = 0;
                foreach (var oar in allOars)
                {
                    CheckedListItem myItem = new CheckedListItem();
                    myItem.Id = i;
                    myItem.Name = oar.Id;
                    myItem.IsEnabled = true;
                    myItem.IsChecked = false;
                    cmbCheckOar.Items.Add(myItem);
                    i++;
                }
            }
            if (cmbCheckOar.Items.Count == 0)
            {
                CheckedListItem myItem = new CheckedListItem();
                myItem.Id = 0;
                myItem.Name = "No OARs found";
                myItem.IsChecked = false;
                myItem.IsEnabled = false;
                cmbCheckOar.Items.Add(myItem);
            }
        }

        public void CopyStructureSelections(SelectPtvItv modelSelection)
        {
            cmbCheckPtv.Items.Clear();
            cmbCheckItv.Items.Clear();
            cmbCheckOar.Items.Clear();
            foreach (CheckedListItem modelPtv in modelSelection.cmbCheckPtv.Items)
            {
                cmbCheckPtv.Items.Add(modelPtv);
            }
            foreach(CheckedListItem modelItv in modelSelection.cmbCheckItv.Items)
            {
                cmbCheckItv.Items.Add(modelItv);
            }
            foreach (CheckedListItem modelItv in modelSelection.cmbCheckItv.Items)
            {
                cmbCheckOar.Items.Add(modelItv);
            }
        }
    }
}
