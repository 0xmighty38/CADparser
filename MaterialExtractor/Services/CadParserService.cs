using System;
using System.Collections.Generic;
using System.Linq;
using MaterialExtractor.Models;
using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.IO;
using ACadSharp.Tables;

namespace MaterialExtractor.Services
{
    public class CadParserService
    {
        public List<Material> ParseCadFile(string filePath)
        {
            var materials = new List<Material>();

            try
            {
                // Load the CAD file (DWG or DXF)
                CadDocument doc;
                string fileExtension = System.IO.Path.GetExtension(filePath).ToLower();
                if (fileExtension == ".dwg")
                {
                    using (var dwgReader = new DwgReader(filePath))
                    {
                        doc = dwgReader.Read();
                    }
                }
                else if (fileExtension == ".dxf")
                {
                    using (var dxfReader = new DxfReader(filePath))
                    {
                        doc = dxfReader.Read();
                    }
                }
                else
                {
                    throw new ArgumentException("Unsupported file format. Only DWG and DXF are supported.");
                }

                // Strategy 1: Extract from Blocks
                foreach (var blockRecord in doc.BlockRecords)
                {
                    string blockName = blockRecord.Name;
                    if (string.IsNullOrWhiteSpace(blockName) || blockName.StartsWith("*")) continue;

                    // Count block references (Insert entities)
                    int blockQuantity = doc.Entities.OfType<Insert>().Count(insert => insert.Block.Name == blockName);
                    if (blockQuantity > 0)
                    {
                        materials.Add(new Material
                        {
                            Name = blockName,
                            Quantity = blockQuantity
                        });
                    }

                    // Check attributes in block references
                    foreach (var insert in doc.Entities.OfType<Insert>().Where(i => i.Block.Name == blockName))
                    {
                        foreach (var attrib in insert.Attributes)
                        {
                            if (attrib.Value.Contains("="))
                            {
                                string[] parts = attrib.Value.Split('=');
                                if (parts.Length == 2 && double.TryParse(parts[1], out double qty))
                                {
                                    materials.Add(new Material
                                    {
                                        Name = $"{blockName} ({attrib.Tag})",
                                        Quantity = qty
                                    });
                                }
                            }
                        }
                    }
                }

                // Strategy 2: Extract from Layers
                foreach (var layer in doc.Layers)
                {
                    string layerName = layer.Name;
                    if (string.IsNullOrWhiteSpace(layerName)) continue;

                    int entityCount = doc.Entities.Count(e => e.Layer.Name == layerName);
                    if (entityCount > 0)
                    {
                        materials.Add(new Material
                        {
                            Name = $"Layer: {layerName}",
                            Quantity = entityCount
                        });
                    }
                }

                // Strategy 3: Extract from Text Entities
                foreach (var text in doc.Entities.OfType<TextEntity>())
                {
                    string textValue = text.Value?.Trim();
                    if (string.IsNullOrWhiteSpace(textValue)) continue;

                    var parts = textValue.Split(new[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && double.TryParse(parts.Last(), out double qty))
                    {
                        string materialName = string.Join(" ", parts.Take(parts.Length - 1));
                        materials.Add(new Material
                        {
                            Name = materialName,
                            Quantity = qty
                        });
                    }
                }

                // Aggregate duplicates
                materials = materials
                    .GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new Material
                    {
                        Name = g.Key,
                        Quantity = g.Sum(m => m.Quantity)
                    })
                    .ToList();

                if (!materials.Any())
                {
                    materials.Add(new Material { Name = "No materials detected", Quantity = 0 });
                }
            }
            catch (Exception ex)
            {
                materials.Clear();
                materials.Add(new Material { Name = $"Error parsing CAD file: {ex.Message}", Quantity = 0 });
            }

            return materials;
        }
    }
}