using System.IO;
using UnityEngine;
using System.Linq;

public class FreightSaveManager
{
    public static IFCFileHandler ListFile;
    public const string SUBDIR = "steveman0.FreightCarts";
    public static string SavePath;
    public static FreightSaveManager instance;


    public FreightSaveManager()
    {
        if (WorldScript.mbIsServer)
        {
            SavePath = DiskWorld.GetWorldsDir() + Path.DirectorySeparatorChar + WorldScript.instance.mWorldData.mPath + Path.DirectorySeparatorChar + SUBDIR + Path.DirectorySeparatorChar;
            if (Directory.Exists(SavePath) == false)
                Directory.CreateDirectory(SavePath);
        }
        instance = this;
        this.ListFileHandler();
    }


    public void ListFileHandler()
    {
        if (WorldScript.mbIsServer)
        {
            string baseFileName = SavePath + "FreightData.dat";

            ListFile = WorldScript.instance.mDiskThread.RegisterManagedFile(new ManagedFileSaveMethod(this.WriteListData), new ManagedFileLoadMethod(this.ReadListData), new ManagedFileConversionMethod(this.ListDataConversion), baseFileName);
            ListFile.MarkReady();
            ListFile.MarkDirty();
        }
        else if (!WorldScript.mbIsServer)
            Debug.LogWarning("SaveBlueprint should only be called by the server!");

    }

    public bool WriteListData(BinaryWriter writer)
    {

        return true;
    }

    public FCFileLoadAttemptResult ReadListData(BinaryReader reader, bool isbackup)
    {

        return FCFileLoadAttemptResult.Successful;
    }

    public void ListDataConversion()
    {

    }

    public void MarkListDataDirty()
    {
        ListFile.MarkDirty();
    }
}
