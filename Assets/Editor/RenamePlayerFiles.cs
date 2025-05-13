using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class RenamePlayerFiles : EditorWindow
{
    [MenuItem("Tools/Rename PLayer To Player")]
    static void ShowWindow()
    {
        GetWindow<RenamePlayerFiles>("Rename PLayer");
    }

    void OnGUI()
    {
        GUILayout.Label("Đổi tên từ 'PLayer' thành 'Player'", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Đổi tên Files và Folders"))
        {
            RenameFilesAndFolders();
        }

        if (GUILayout.Button("Cập nhật tham chiếu trong Scripts"))
        {
            UpdateScriptReferences();
        }
    }

    private void RenameFilesAndFolders()
    {
        // Danh sách các file và thư mục cần đổi tên
        List<string> itemsToRename = new List<string>
        {
            "Assets/Prefabs/PLayer",
            "Assets/Prefabs/PLayer/PLayerAvatar.prefab",
            "Assets/Prefabs/PLayer/PLayerAvatar.prefab.meta",
            "Assets/Prefabs/PLayer/PLayerSlot.prefab",
            "Assets/Prefabs/PLayer/PLayerSlot.prefab.meta",
            "Assets/Scripts/PLayerData.cs",
            "Assets/Scripts/PLayerData.cs.meta"
        };

        foreach (string path in itemsToRename)
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                string newPath = path.Replace("PLayer", "Player");
                string directory = Path.GetDirectoryName(newPath);
                
                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                try
                {
                    // Di chuyển file hoặc thư mục
                    if (File.Exists(path))
                    {
                        File.Move(path, newPath);
                        Debug.Log($"Đã đổi tên file: {path} -> {newPath}");
                    }
                    else if (Directory.Exists(path))
                    {
                        // Đối với thư mục, tạo thư mục mới trước
                        if (!Directory.Exists(newPath))
                        {
                            Directory.CreateDirectory(newPath);
                            
                            // Di chuyển các file con từ thư mục cũ sang thư mục mới
                            foreach (string filePath in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                            {
                                string relativePath = filePath.Substring(path.Length);
                                string newFilePath = newPath + relativePath;
                                
                                string newFileDir = Path.GetDirectoryName(newFilePath);
                                if (!Directory.Exists(newFileDir))
                                {
                                    Directory.CreateDirectory(newFileDir);
                                }
                                
                                File.Move(filePath, newFilePath);
                            }
                            
                            // Xóa thư mục cũ sau khi di chuyển tất cả các file
                            Directory.Delete(path, true);
                            Debug.Log($"Đã đổi tên thư mục: {path} -> {newPath}");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Lỗi khi đổi tên {path}: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"Không tìm thấy file hoặc thư mục: {path}");
            }
        }

        AssetDatabase.Refresh();
    }

    private void UpdateScriptReferences()
    {
        // Tìm tất cả các script C#
        string[] scriptFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        
        foreach (string scriptPath in scriptFiles)
        {
            string content = File.ReadAllText(scriptPath);
            bool hasChanges = false;
            
            // Sửa các tham chiếu đến class hoặc file
            if (content.Contains("PLayerData"))
            {
                content = content.Replace("PLayerData", "PlayerData");
                hasChanges = true;
            }
            
            if (content.Contains("PLayerAvatar"))
            {
                content = content.Replace("PLayerAvatar", "PlayerAvatar");
                hasChanges = true;
            }
            
            if (content.Contains("PLayerSlot"))
            {
                content = content.Replace("PLayerSlot", "PlayerSlot");
                hasChanges = true;
            }
            
            // Sửa các tham chiếu đến thư mục
            if (content.Contains("Prefabs/PLayer"))
            {
                content = content.Replace("Prefabs/PLayer", "Prefabs/Player");
                hasChanges = true;
            }
            
            // Lưu lại file nếu có thay đổi
            if (hasChanges)
            {
                File.WriteAllText(scriptPath, content);
                Debug.Log($"Đã cập nhật tham chiếu trong script: {scriptPath}");
            }
        }
        
        AssetDatabase.Refresh();
    }
} 