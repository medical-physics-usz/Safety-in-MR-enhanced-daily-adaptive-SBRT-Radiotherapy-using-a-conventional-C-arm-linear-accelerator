import os
import shutil
import time

import SimpleITK as sitk
import matplotlib.pyplot as plt
import pydicom as dicom


def move_original_CT(folder_path):
    #target_dir = os.path.join(folder_path, "original_CT")  # Create a path to the "original_CT" sub-folder
    
    parent_dir = os.path.abspath(os.path.join(folder_path, os.pardir))
    original_folder_name = os.path.basename(folder_path)
    original_folder_name = original_folder_name + "_original_CT"
    target_dir = os.path.join(parent_dir, original_folder_name) 
    
    # Create the sub-folder if it doesn't exist
    if os.path.exists(target_dir):
        shutil.rmtree(target_dir)
    os.makedirs(target_dir)
    
    # Loop through all the files in the current directory
    for file_name in os.listdir(folder_path):
        # Check if the file starts with "CT" and ends with ".dcm"
        if file_name.startswith("CT") and file_name.endswith(".dcm"):
            # Move the file to the target directory
            source_path = os.path.join(folder_path, file_name)
            destination_path = os.path.join(target_dir, file_name)
            shutil.move(source_path, destination_path)

    return target_dir
    
def load_dicom_series(folder_path):
    # Get list of files in the directory
    dicom_files = [f for f in os.listdir(folder_path) if f.startswith("CT") and f.endswith(".dcm")]

    # Ensure there are matching DICOM files
    if not dicom_files:
        raise FileNotFoundError("No DICOM files starting with 'CT' and ending with '.dcm' found in the folder.")

    # Sort files in case order matters for DICOM series
    dicom_files = sorted(dicom_files)

    # Convert relative file paths to absolute paths
    dicom_file_paths = [os.path.join(folder_path, f) for f in dicom_files]

    # Load the series using SimpleITK
    reader = sitk.ImageSeriesReader()
    reader.SetFileNames(dicom_file_paths)
    image = reader.Execute()
    
    return image

def delete_folder(folder_path):
    """
    Deletes the specified folder and all its contents.

    Args:
        folder_path (str): The path to the folder to delete.

    Raises:
        FileNotFoundError: If the folder does not exist.
        PermissionError: If there are insufficient permissions.
    """
    try:
        if os.path.exists(folder_path):
            shutil.rmtree(folder_path)
            # print(f"Folder '{folder_path}' and all its contents have been deleted.")
        # else:
        #     print(f"Folder '{folder_path}' does not exist.")
    except Exception as e:
        print(f"An error occurred: {e}")

def resample_image_to_resolution(image, new_spacing):
    """
    Resamples a 3D SimpleITK image to the given user-defined resolution (voxel dimensions).
    
    Parameters:
    - image: SimpleITK image (3D)
    - new_spacing: List or tuple with the new voxel dimensions (resolution) in mm [x, y, z]
    
    Returns:
    - Resampled 3D image with the new voxel spacing.
    """
    # Get original spacing and size
    original_spacing = image.GetSpacing()  # Current voxel dimensions
    original_size = image.GetSize()        # Current image size (number of voxels)
    
    # Calculate the new size based on the new spacing
    # New size (number of voxels) is calculated to preserve physical image dimensions
    new_size = [
        int(round(osz * ospc / nspc)) for osz, ospc, nspc in zip(original_size, original_spacing, new_spacing)
    ]
    
    # Define the resampler
    resampler = sitk.ResampleImageFilter()
    
    # Set the new spacing (voxel dimensions)
    resampler.SetOutputSpacing(new_spacing)
    
    # Set the new size (number of voxels) based on the new spacing
    resampler.SetSize(new_size)
    
    # Set the interpolator (use linear interpolation for continuous image values)
    resampler.SetInterpolator(sitk.sitkLinear)
    resampler.SetDefaultPixelValue(-1024)
    
    # Set the output origin and direction the same as the original image
    resampler.SetOutputOrigin(image.GetOrigin())
    resampler.SetOutputDirection(image.GetDirection())
    
    # Resample the image
    resampled_image = resampler.Execute(image)
    
    # Use supported formats for DICOM
    resampled_image = sitk.Cast(resampled_image, sitk.sitkUInt16)
    
    return resampled_image


def save_resampled_image_as_dicom(resampled_CT, input_folder, output_folder):
    """
    Save the resampled 3D image as a series of DICOM files, with most DICOM tags copied from the original image.
    
    Parameters:
    - resampled_CT: The resampled 3D SimpleITK image to save.
    - original_CT: The original 3D SimpleITK image to copy DICOM tags from.
    - output_folder: The folder to save the series of DICOM slices.
    """
    
    # Load the original CT to get the DICOM tags
    dicom_files = [f for f in os.listdir(input_folder) if f.startswith("CT") and f.endswith(".dcm")]
    
    # Ensure there are matching DICOM files
    if not dicom_files:
        raise FileNotFoundError("No DICOM files starting with 'CT' and ending with '.dcm' found in the folder.")
    
    # Sort files in case order matters for DICOM series
    dicom_files = sorted(dicom_files)
    
    # Convert relative file paths to absolute paths
    dicom_file_paths = [os.path.join(input_folder, f) for f in dicom_files]
    
    # Load the series using SimpleITK
    reader = sitk.ImageSeriesReader()
    reader.SetFileNames(dicom_file_paths)
    original_CT = reader.Execute()
    
    # Load the first slice with pydicom to retreive tags
    original_CT_pydicom = dicom.dcmread(dicom_file_paths[0])
       
    writer = sitk.ImageFileWriter()
    # Use the study/series/frame of reference information given in the meta-data
    # dictionary and not the automatically generated information from the file IO
    writer.KeepOriginalImageUIDOn()
    
    # Copy relevant tags from the original meta-data dictionary (private tags are
    # also accessible). 
    # Series DICOM tags to copy
    series_tag_values = [
        # ("0008|0008", original_CT_pydicom[0x00080008].value), # Image type --> it does not work here because of str(value), added manually afterwards
        ("0008|0070", original_CT_pydicom[0x00080070].value), # Manifacturer
        ("0018|0060", original_CT_pydicom[0x00180060].value), # kVp
        ("0008|103e", original_CT_pydicom[0x0008103e].value), # Series description
        ("0010|0010", original_CT_pydicom[0x00100010].value), # Patient Name
        ("0010|0020", original_CT_pydicom[0x00100020].value), # Patient ID
        ("0010|0030", original_CT_pydicom[0x00100030].value), # Patient Birth Date
        ("0010|0040", original_CT_pydicom[0x00100040].value), # Patient Sex
        ("0020|000d", original_CT_pydicom[0x0020000d].value), # Study Instance UID, for machine consumption
        ("0020|000e", original_CT_pydicom[0x0020000e].value), # Series istance UID
        ("0020|0010", original_CT_pydicom[0x00200010].value), # Study ID, for human consumption
        ("0008|0020", original_CT_pydicom[0x00080020].value), # Study Date
        ("0008|0030", original_CT_pydicom[0x00080030].value), # Study Time
        ("0008|0050", original_CT_pydicom[0x00080050].value), # Accession Number
        ("0008|0060", original_CT_pydicom[0x00080060].value), # Modality
        ("0028|1050", original_CT_pydicom[0x00281050].value), # Window center
        ("0028|1051", original_CT_pydicom[0x00281051].value), # Window width
        ("0028|1052", original_CT_pydicom[0x00281052].value), # Rescale Intercept
        ("0028|1053", original_CT_pydicom[0x00281053].value), # Rescale Slope
        ("0028|1054", original_CT_pydicom[0x00281054].value), # Rescale Type
        ("0018|5100", original_CT_pydicom[0x00185100].value), # Patient Position
        ("0020|0052", original_CT_pydicom[0x00200052].value), # Frame of Reference UID
        ("0008|0023", original_CT_pydicom[0x00080023].value), # Image date
        ("0008|0030", original_CT_pydicom[0x00080030].value), # Stdy time
        ("0008|0031", original_CT_pydicom[0x00080031].value), # Series time
        # ("0008|0032", original_CT_pydicom[0x00080032].value), # Image time
        ("0008|0080", original_CT_pydicom[0x00080080].value), # Institution
        #("0008|0081", original_CT_pydicom[0x00080081].value), # Institution address
        ("0008|1010", original_CT_pydicom[0x00081010].value), # Station name
        ("0008|1030", original_CT_pydicom[0x00081030].value), # Study description
        ("0008|1090", original_CT_pydicom[0x00081090].value), # Manifacturer model name
        # ("0008|1150", original_CT_pydicom[0x00081150].value), # SOP UID
        # ("0008|1155", original_CT_pydicom[0x00081155].value), # SOP UID
        ("0018|0015", original_CT_pydicom[0x00180015].value), # Body part
        ("0020|0011", original_CT_pydicom[0x00200011].value), # Series number
        # ("0020|0037", original_CT_pydicom[0x00200037].value), # Patient orientation
        ("0020|0060", original_CT_pydicom[0x00200060].value), # Laterality
        ("0020|4000", original_CT_pydicom[0x00204000].value), # Comments
      

        ]
    
    spacing_resampled = resampled_CT.GetSpacing()  # (spacing_x, spacing_y, spacing_z)
       

    for i in range(resampled_CT.GetDepth()):
        image_slice = resampled_CT[:, :, i]
        # Tags shared by the series.
        for tag, value in series_tag_values:
            image_slice.SetMetaData(tag, str(value))
        # Slice specific tags.
        #   Instance Creation Date
        image_slice.SetMetaData("0008|0012", time.strftime("%Y%m%d"))
        #   Instance Creation Time
        image_slice.SetMetaData("0008|0013", time.strftime("%H%M%S"))
        #   Image Position (Patient)
        image_slice.SetMetaData(
            "0020|0032",
            "\\".join(map(str, resampled_CT.TransformIndexToPhysicalPoint((0, 0, i)))),
        )
        #   Instance Number
        image_slice.SetMetaData("0020|0013", str(i))
        # Set slice thickness (assuming uniform thickness)
        image_slice.SetMetaData("0018|0050", f"{spacing_resampled[2]:.6f}")  # Slice Thickness
        
        # Image type
        image_slice.SetMetaData("0008|0008", "DERIVED\\SECONDARY\\AXIAL")  
    
        # Write to the output directory and add the extension dcm, to force writing
        # in DICOM format.
        writer.SetFileName(os.path.join(output_folder, "CT_Resampled_Slice_" + str(i) + ".dcm"))
        writer.Execute(image_slice)  


def plot_histogram(image1,image2):
    """
    Plot a histogram of the Hounsfield Unit (HU) values in the CT image.
    
    Parameters:
    - image: The SimpleITK image containing the CT data.
    """
    # Convert the SimpleITK image to a NumPy array
    image1_array = sitk.GetArrayFromImage(image1)  # Shape: (depth, height, width)
    image2_array = sitk.GetArrayFromImage(image2) 
    
    # Flatten the 3D array to 1D for histogram
    hu_values1 = image1_array.flatten()
    hu_values2 = image2_array.flatten()
    
    # Plot the histogram
    fig, axs = plt.subplots(3, 1, figsize=(10, 8))
    
    # Plot the overlapping histograms
    axs[0].hist(hu_values1, bins=256, range=(-999, 3071), color='blue', alpha=0.5,label="Original CT", density=True)  # HU range
    axs[0].hist(hu_values2, bins=256, range=(-999, 3071), color='red', alpha=0.5,label="Resampled CT", density=True)  # HU range
    axs[0].set_title("Resampling completed.\nVisually confirm that the HU are similar and close this window.\nImport in Eclipse the resampled CT.\n\nBoth CT in the same plot:")
    axs[0].set_xlabel('Hounsfield Unit (HU)')
    axs[0].set_ylabel('Frequency')
    # axs[0].set_yscale('log')
    axs[0].legend()
    
    
    # Plot one at a time
    axs[1].hist(hu_values1, bins=256, range=(-999, 3071), color='blue', alpha=0.5)  # HU range
    axs[1].set_title("Original CT")
    
    axs[2].hist(hu_values2, bins=256, range=(-999, 3071), color='red', alpha=0.5)  # HU range
    axs[2].set_title("Resampled CT")
    
    plt.tight_layout()
    plt.show()

#########
# Main program starting here
#########
current_folder = os.getcwd()

# Move the original CT to another folder
moved_original_CT_folder = move_original_CT(current_folder)

# Load the data in the folder where the exe is executed
original_CT = load_dicom_series(moved_original_CT_folder)

# Resample to a user-defined resolution
new_spacing = [1.0, 1.0, 1.0]  # in mm
resampled_CT = resample_image_to_resolution(original_CT, new_spacing)
     
# Write the resampled CT to the subfolder
save_resampled_image_as_dicom(resampled_CT, moved_original_CT_folder, current_folder)

# Plot histograms to ensure consistency of HU
resampled_CT_reload = load_dicom_series(current_folder)
plot_histogram(original_CT,resampled_CT_reload)

# Delete the original CT to avoid problems
delete_folder(moved_original_CT_folder)

    
    
    
    
    
    
    
    
    
    
    
    
    
    
    

    
    
    
    
    
    
    
    
    
    
    