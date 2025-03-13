using System;
using System.Collections.Generic;
using System.Linq;
using MaterialExtractor.Models;
using netDxf;
using netDxf.Entities;

namespace MaterialExtractor.Services
{
    public class DxfParserService
    {
        public List<Material> ParseDxfFile(string filePath)
        {
            var materials = new List<Material>();

            try
            {
                // Load the DXF file
                DxfDocument doc = DxfDocument.Load(filePath);

                // Strategy 1: Extract from Blocks
                foreach (var block in doc.Blocks)
                {
                    string blockName = block.Name;
                    if (string.IsNullOrWhiteSpace(blockName) || blockName.StartsWith("*")) continue;

                    // Count block references
                    int blockQuantity = doc.Entities.Inserts.Count(insert => insert.Block.Name == blockName);
                    if (blockQuantity > 0)
                    {
                        materials.Add(new Material
                        {
                            Name = blockName,
                            Quantity = blockQuantity
                        });
                    }

                    // Check attributes in block references
                    foreach (var insert in doc.Entities.Inserts.Where(i => i.Block.Name == blockName))
                    {
                        foreach (var attrib in insert.Attributes) // Changed from .Values to direct iteration
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

                    int entityCount = doc.Entities.All.Count(e => e.Layer.Name == layerName);
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
                foreach (var text in doc.Entities.Texts)
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
                materials.Add(new Material { Name = $"Error parsing DXF: {ex.Message}", Quantity = 0 });
            }

            return materials;
        }
    }
}