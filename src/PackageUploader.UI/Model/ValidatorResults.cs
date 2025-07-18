﻿using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PackageUploader.UI.Model
{
    public class ValidatorResults
    {
        public Guid ProductId { get; set; } = Guid.Empty;
        public Guid ContentId { get; set; } = Guid.Empty;
        public Guid BuildId { get; set; } = Guid.Empty;
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int TotalWarnings { get; set; } = -1;
        public int TotalErrors { get; set; } = -1;
        public bool Succeeded { get; set; } = false;
        public List<ValidatorComponent> Components { get; set; } = new List<ValidatorComponent>();

        public ValidatorResults()
        {
            this.Reset();
        }

        public void Reset()
        {
            ProductId = Guid.Empty;
            ContentId = Guid.Empty;
            BuildId = Guid.Empty;
            TotalWarnings = -1;
            TotalErrors = -1;
            Succeeded = false;
            Components.Clear();
            Type = string.Empty;
            Name = string.Empty;
        }

        public string ErrorToString()
        {
            StringBuilder sb = new StringBuilder();
            int ErrorCount = 0;
            foreach (var component in Components)
            {
                foreach (var item in component.Items)
                {
                    if (item.Type == ValidatorTestResultType.Failure)
                    {
                        sb.AppendLine($"{++ErrorCount}. {item}");
                    }
                }
            }
            return sb.ToString();
        }

        public void Parse(string inputFile)
        {
            this.Reset();
            if (String.IsNullOrEmpty(inputFile) || !File.Exists(inputFile))
            {
                throw new InvalidDataException("Validator result file does not exist");
            }

            XmlReaderSettings settings = new();
            using (var reader = XmlReader.Create(inputFile, settings))
            {
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (String.Equals(reader.Name, "project", StringComparison.OrdinalIgnoreCase))
                            {
                                this.ParseProjectTag(reader.ReadSubtree());
                            }
                            if (String.Equals(reader.Name, "ValidationSummary", StringComparison.OrdinalIgnoreCase))
                            {
                                this.ParseValidationSummary(reader.ReadSubtree());
                            }
                            if (String.Equals(reader.Name, "testresult", StringComparison.OrdinalIgnoreCase))
                            {
                                this.ParseTestResult(reader.ReadSubtree());
                            }
                            break;
                    }
                }
            }
        }

        private void ParseProjectTag(XmlReader reader)
        {
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (String.Equals(reader.Name, "Name", StringComparison.OrdinalIgnoreCase))
                        {
                            this.Name = reader.ReadElementContentAsString();
                        }
                        else if (String.Equals(reader.Name, "Type", StringComparison.OrdinalIgnoreCase))
                        {
                            this.Type = reader.ReadElementContentAsString();
                        }
                        else if (String.Equals(reader.Name, "ProductId", StringComparison.OrdinalIgnoreCase))
                        {
                            this.ProductId = Guid.Parse(reader.ReadElementContentAsString());
                        }
                        else if (String.Equals(reader.Name, "ContentId", StringComparison.OrdinalIgnoreCase))
                        {
                            this.ContentId = Guid.Parse(reader.ReadElementContentAsString());
                        }
                        else if (String.Equals(reader.Name, "BuildId", StringComparison.OrdinalIgnoreCase))
                        {
                            this.BuildId = Guid.Parse(reader.ReadElementContentAsString());
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (String.Equals(reader.Name, "project", StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }
                        break;
                }
            }
        }
        private void ParseValidationSummary(XmlReader reader)
        {
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (String.Equals(reader.Name, "TotalWarnings", StringComparison.OrdinalIgnoreCase))
                        {
                            this.TotalWarnings = reader.ReadElementContentAsInt();
                        }
                        else if (String.Equals(reader.Name, "TotalFailures", StringComparison.OrdinalIgnoreCase))
                        {
                            this.TotalErrors = reader.ReadElementContentAsInt();
                        }
                        else if (String.Equals(reader.Name, "Result", StringComparison.OrdinalIgnoreCase))
                        {
                            this.Succeeded = !String.Equals(reader.ReadElementContentAsString(), "Fail", StringComparison.OrdinalIgnoreCase);
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (String.Equals(reader.Name, "ValidationSummary", StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }
                        break;
                }
            }
        }
        private void ParseTestResult(XmlReader reader)
        {
            ValidatorComponent component = new ValidatorComponent();
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        {
                            if (String.Equals(reader.Name, "component", StringComparison.OrdinalIgnoreCase))
                            {
                                // First, determine if this is a specialized component
                                component.Component = reader.ReadElementContentAsString();
                                if (String.Equals(component.Component, "Tools Check", StringComparison.OrdinalIgnoreCase))
                                {
                                    component = new ValidatorComponentToolsCheck();
                                    component.Component = "Tools Check";
                                    break;
                                }
                            }
                            // Next, parse these known results type and put the into the component
                            else if (String.Equals(reader.Name, "info", StringComparison.OrdinalIgnoreCase) && reader.HasAttributes)
                            {
                                ValidatorTestResult result = new ValidatorTestResult();
                                result.Type = ValidatorTestResultType.Info;
                                result.Id = reader.GetAttribute("Id") ?? String.Empty;
                                result.Message = reader.ReadElementContentAsString();
                                component.Items.Add(result);
                            }
                            else if (String.Equals(reader.Name, "failure", StringComparison.OrdinalIgnoreCase) && reader.HasAttributes)
                            {

                                ValidatorTestResult failureResult = new ValidatorTestResult();
                                failureResult.Type = ValidatorTestResultType.Failure;
                                failureResult.Id = reader.GetAttribute("Id") ?? String.Empty;
                                failureResult.Message = reader.ReadElementContentAsString();
                                component.Items.Add(failureResult);
                            }
                            else if (String.Equals(reader.Name, "warning", StringComparison.OrdinalIgnoreCase) && reader.HasAttributes)
                            {

                                ValidatorTestResult warningResult = new ValidatorTestResult();
                                warningResult.Type = ValidatorTestResultType.Warning;
                                warningResult.Id = reader.GetAttribute("Id") ?? String.Empty;
                                warningResult.Message = reader.ReadElementContentAsString();
                                component.Items.Add(warningResult);
                            }
                            // finally, give the conponent itself a chance to read this node
                            component.ParseResultTag(reader);
                            break;
                        }
                    case XmlNodeType.EndElement:
                        if (String.Equals(reader.Name, "testresult", StringComparison.OrdinalIgnoreCase))
                        {
                            this.Components.Add(component);
                            return;
                        }
                        break;
                }
            }
        }
    }

    public class ValidatorComponent 
    {
        public string Component { get; set; } = string.Empty;
        public List<ValidatorTestResult> Items { get; set; } = new List<ValidatorTestResult>();

        public virtual void ParseResultTag(XmlReader reader) { }
    }

    public class ValidatorComponentToolsCheck : ValidatorComponent
    {
        public string MakePkg_Version { get; set; } = string.Empty;
        public string PackagingServices_Version { get; set; } = string.Empty;
        public string XSAPI_Version { get; set; } = string.Empty;
        public string XCRDAPI_Version { get; set; } = string.Empty;
        public string XCITREE_Version { get; set; } = string.Empty;
        public string Windows_Version { get; set; } = string.Empty;
        public string Windows_10_SDK { get; set; } = string.Empty;
        public string GRTS_Version { get; set; } = string.Empty;
        public string XCAPI_Version { get; set; } = string.Empty;

        public override void ParseResultTag(XmlReader reader)
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    if (String.Equals(reader.Name, "MakePkg_Version", StringComparison.OrdinalIgnoreCase))
                    {
                        this.MakePkg_Version = reader.ReadElementContentAsString();
                    }
                    else if (String.Equals(reader.Name, "PackagingServices_Version", StringComparison.OrdinalIgnoreCase))
                    {
                        this.PackagingServices_Version = reader.ReadElementContentAsString();
                    }
                    else if (String.Equals(reader.Name, "XSAPI_Version", StringComparison.OrdinalIgnoreCase))
                    {
                        this.XSAPI_Version = reader.ReadElementContentAsString();
                    }
                    else if (String.Equals(reader.Name, "XCRDAPI_Version", StringComparison.OrdinalIgnoreCase))
                    {
                        this.XCRDAPI_Version = reader.ReadElementContentAsString();
                    }
                    else if (String.Equals(reader.Name, "XCITREE_Version", StringComparison.OrdinalIgnoreCase))
                    {
                        this.XCITREE_Version = reader.ReadElementContentAsString();
                    }
                    else if (String.Equals(reader.Name, "Windows_Version", StringComparison.OrdinalIgnoreCase))
                    {
                        this.Windows_Version = reader.ReadElementContentAsString();
                    }
                    else if (String.Equals(reader.Name, "Windows_10_SDK", StringComparison.OrdinalIgnoreCase))
                    {
                        this.Windows_10_SDK = reader.ReadElementContentAsString();
                    }
                    else if (String.Equals(reader.Name, "GRTS_Version", StringComparison.OrdinalIgnoreCase))
                    {
                        this.GRTS_Version = reader.ReadElementContentAsString();
                    }
                    else if (String.Equals(reader.Name, "XCAPI_Version", StringComparison.OrdinalIgnoreCase))
                    {
                        this.XCAPI_Version = reader.ReadElementContentAsString();
                    }
                    break;
            }
        }
    }

    public class ValidatorTestResult
    {
        public string Id { get; set; } = string.Empty;
        public ValidatorTestResultType Type { get; set; } = ValidatorTestResultType.None;
        public string Message { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{Id}: {Message}";
        }
    }

    public enum ValidatorTestResultType
    {
        None,
        Info,
        Warning,
        Failure
    }
}
