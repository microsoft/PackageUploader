using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PackageUploader.UI.Model
{
    class GameConfigModel
    {
        public Identity Identity { get; set; } = new Identity();
        public List<Executable> Executables { get; set; } = new List<Executable>();
        public ShellVisuals ShellVisuals { get; set; } = new ShellVisuals();
        public List<Protocol> Protocols { get; set; } = new List<Protocol>();

        public string MSAAppId { get; set; } = string.Empty;
        public string TitleId { get; set; } = string.Empty;
        public string StoreId { get; set; } = string.Empty;
        public long PersistentLocalStorage { get; set; } = 0;

        public GameConfigModel(string? configPath)
        {
            if(String.IsNullOrEmpty(configPath) || !File.Exists(configPath))
            {
                throw new InvalidDataException("MicrosoftGame.config file either does not exist or configPath variable is null/empty");
            }

            XmlReaderSettings settings = new XmlReaderSettings();
            var parentDirectory = Directory.GetParent(configPath);

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
                                Executable exe = new Executable(); //copilot being scarrily good here
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
                            if (reader.Name == "PersistentLocalStorage")
                            {
                                //need to then get the next id which should be "SizeXB", and then read in the content, multiplying it appropriately
                                //this.PersistentLocalStorage = reader.ReadElementContentAsLong();
                                var sizeReader = reader.ReadSubtree();
                                while (sizeReader.Read())
                                {
                                    if(sizeReader.Name == "SizeKB")
                                    {
                                        this.PersistentLocalStorage = sizeReader.ReadElementContentAsLong() * 1024;
                                    }
                                    if (sizeReader.Name == "SizeMB")
                                    {
                                        this.PersistentLocalStorage = sizeReader.ReadElementContentAsLong() * 1024 * 1024;
                                    }
                                    if(sizeReader.Name == "SizeGB")
                                    {
                                        this.PersistentLocalStorage = sizeReader.ReadElementContentAsLong() * 1024 * 1024 * 1024;
                                    }
                                }
                                reader.Skip(); //hopefully this works
                            }
                            if (reader.Name == "Protocol" && reader.HasAttributes)
                            {
                                Protocol protocol = new Protocol();
                                while (reader.MoveToNextAttribute())
                                {
                                    if (reader.Name == "Name")
                                    {
                                        protocol.Name = reader.Value;
                                    }
                                }
                                this.Protocols.Add(protocol);
                                reader.MoveToElement();
                            }
                            break;
                    }
                }
            }
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

    public class Protocol
    {
        public string Name { get; set; } = string.Empty;
    }
}
