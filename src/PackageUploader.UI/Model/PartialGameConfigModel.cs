using System.IO;
using System.Xml;

namespace PackageUploader.UI.Model
{
    class PartialGameConfigModel
    {
        public Identity Identity { get; set; } = new Identity();
        public List<Executable> Executables { get; set; } = [];
        public ShellVisuals ShellVisuals { get; set; } = new ShellVisuals();

        public string MSAAppId { get; set; } = string.Empty;
        public string TitleId { get; set; } = string.Empty;
        public string StoreId { get; set; } = string.Empty;

        public PartialGameConfigModel(string? configPath)
        {
            if (String.IsNullOrEmpty(configPath) || !File.Exists(configPath))
            {
                throw new InvalidDataException("MicrosoftGame.config file either does not exist or configPath variable is null/empty");
            }

            XmlReaderSettings settings = new();
            var parentDirectory = Directory.GetParent(configPath) ?? throw new InvalidDataException("Failed to get parent directory of MicrosoftGame.config");

            using (var reader = XmlReader.Create(configPath, settings))
            {
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name == "ShellVisuals" && reader.HasAttributes) // HasAttributes should be true
                            {
                                while (reader.MoveToNextAttribute())
                                {
                                    if (reader.Name == "StoreLogo")
                                    {
                                        this.ShellVisuals.StoreLogo = Path.Combine(parentDirectory.FullName, reader.Value);
                                    }
                                    if(reader.Name == "Square150x150Logo")
                                    {
                                        this.ShellVisuals.Square150x150Logo = Path.Combine(parentDirectory.FullName, reader.Value);
                                    }
                                    if (reader.Name == "Square44x44Logo")
                                    {
                                        this.ShellVisuals.Square44x44Logo = Path.Combine(parentDirectory.FullName, reader.Value);
                                    }
                                    if (reader.Name == "SplashScreenImage")
                                    {
                                        this.ShellVisuals.SplashScreenImage = Path.Combine(parentDirectory.FullName, reader.Value);
                                    }
                                    if (reader.Name == "DefaultDisplayName")
                                    {
                                        this.ShellVisuals.DefaultDisplayName = reader.Value;
                                    }
                                    if (reader.Name == "PublisherDisplayName")
                                    {
                                        this.ShellVisuals.PublisherDisplayName = reader.Value;
                                    }
                                    if (reader.Name == "Description")
                                    {
                                        this.ShellVisuals.Description = reader.Value;
                                    }
                                }
                                reader.MoveToElement();
                            }
                            if (reader.Name == "Identity" && reader.HasAttributes)
                            {
                                while (reader.MoveToNextAttribute())
                                {
                                    if (reader.Name == "Name")
                                    {
                                        this.Identity.Name = reader.Value;
                                    }
                                    if(reader.Name == "Publisher")
                                    {
                                        this.Identity.Publisher = reader.Value;
                                    }
                                    if (reader.Name == "Version")
                                    {
                                        this.Identity.Version = reader.Value;
                                    }
                                }
                                reader.MoveToElement();
                            }
                            if(reader.Name == "Executable" && reader.HasAttributes)
                            {
                                Executable exe = new();
                                while (reader.MoveToNextAttribute())
                                {
                                    if (reader.Name == "Name")
                                    {
                                        exe.Name = reader.Value;
                                    }
                                    if (reader.Name == "Id")
                                    {
                                        exe.Id = reader.Value;
                                    }
                                    if (reader.Name == "TargetDeviceFamily")
                                    {
                                        exe.TargetDeviceFamily = reader.Value;
                                    }
                                }
                                this.Executables.Add(exe);
                                reader.MoveToElement();
                            }
                            if (reader.Name == "StoreId")
                            {
                                this.StoreId = reader.ReadElementContentAsString();
                            }
                            if(reader.Name == "TitleId")
                            {
                                this.TitleId = reader.ReadElementContentAsString();
                            }
                            if (reader.Name == "MSAAppId")
                            {
                                this.MSAAppId = reader.ReadElementContentAsString();
                            }
                            break;
                    }
                }
            }
        }

        public string GetDeviceFamily()
        {
            string targetDeviceFamily = string.Empty;
            foreach (var exe in Executables)
            {
                if (string.IsNullOrEmpty(targetDeviceFamily))
                {
                    targetDeviceFamily = exe.TargetDeviceFamily;
                }
                else if (!string.IsNullOrEmpty(exe.TargetDeviceFamily) && targetDeviceFamily != exe.TargetDeviceFamily)
                {
                    throw new Exception("The MicrosoftGame.config has multiple target device families");
                }
            }

            if (string.IsNullOrEmpty(targetDeviceFamily))
            {
                throw new Exception("The MicrosoftGame.config does not have a target device family");
            }

            return targetDeviceFamily == "PC" ? "PC" : "Console";
        }
    }

    public class Identity
    {
        public string Name { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }

    public class Executable
    {
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string TargetDeviceFamily { get; set; } = string.Empty;
    }

    public class ShellVisuals
    {
        public string DefaultDisplayName { get; set; } = string.Empty;
        public string PublisherDisplayName { get; set; } = string.Empty;
        public string StoreLogo { get; set; } = string.Empty;
        public string Square150x150Logo { get; set; } = string.Empty;
        public string Square44x44Logo { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SplashScreenImage { get; set; } = string.Empty;
    }
}
