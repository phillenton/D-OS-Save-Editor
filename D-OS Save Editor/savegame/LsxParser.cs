using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace D_OS_Save_Editor
{
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    public class LsxParser
    {
        /// <summary>
        /// LSX stores &lt;attribute id="..." type="..." value="..." /&gt;; value is not always Attributes[1]. Read the value attribute by name.
        /// </summary>
        private static string GetLsxAttributeValue(XmlNode attributeElement)
        {
            if (attributeElement?.Attributes == null) return null;
            var byName = attributeElement.Attributes["value"];
            if (byName != null) return byName.Value;
            foreach (XmlAttribute a in attributeElement.Attributes)
            {
                if (string.Equals(a.LocalName, "value", StringComparison.OrdinalIgnoreCase))
                    return a.Value;
            }
            return null;
        }

        /// <summary>
        /// Type 28 (TranslatedString) often stores only <c>handle</c> in the save; resolved <c>value</c> may be absent until load.
        /// </summary>
        private static string GetLsxAttributeValueOrHandle(XmlNode attributeElement)
        {
            var v = GetLsxAttributeValue(attributeElement);
            if (!string.IsNullOrEmpty(v)) return v;
            if (attributeElement?.Attributes == null) return null;
            var h = attributeElement.Attributes["handle"];
            if (h != null) return h.Value;
            foreach (XmlAttribute a in attributeElement.Attributes)
            {
                if (string.Equals(a.LocalName, "handle", StringComparison.OrdinalIgnoreCase))
                    return a.Value;
            }
            return null;
        }

        /// <summary>
        /// Sets the LSX <c>value=</c> attribute (not a fixed index — attribute order varies).
        /// </summary>
        private static void SetLsxAttributeValue(XmlNode parentNode, string attributeId, string value)
        {
            var attrEl = parentNode.SelectSingleNode($"attribute [@id='{attributeId}']");
            if (attrEl?.Attributes == null)
                throw new InvalidOperationException($"Missing attribute id='{attributeId}' on node.");
            var v = attrEl.Attributes["value"];
            if (v != null)
            {
                v.Value = value;
                return;
            }

            foreach (XmlAttribute a in attrEl.Attributes)
            {
                if (string.Equals(a.LocalName, "value", StringComparison.OrdinalIgnoreCase))
                {
                    a.Value = value;
                    return;
                }
            }

            throw new InvalidOperationException($"Attribute id='{attributeId}' has no value=.");
        }

        #region read file
        public static List<string> GenerationBoostCollector;
        public static List<string> StatsBoostsCollector;

        public static Player[] ParsePlayer(XmlDocument doc)
        {
            GenerationBoostCollector = new List<string>();
            StatsBoostsCollector = new List<string>();
            
            // find player data
            var playerData = doc.DocumentElement.SelectNodes("//*[@id='PlayerData']");

            if (playerData == null)
                throw new XmlException("Unable to find any player data in the savegame.");

            var players = new Player[playerData.Count];
            Parallel.For(0, playerData.Count, i =>
            {
                try
                {
                    players[i] = ParsePlayer(playerData[i]);
                }
                catch (Exception e)
                {
                    throw new PlayerParserException(e, playerData[i]);
                }

                // BFS: top-level inventory, then each nested container (child items' Parent = container's NestedInventoryId)
                var itemList = new List<Item>();
                var nodeList = new List<XmlNode>();
                var expandQueue = new Queue<string>();
                var expandedInventories = new HashSet<string>();
                expandQueue.Enqueue(players[i].InventoryId);

                while (expandQueue.Count > 0)
                {
                    var invId = expandQueue.Dequeue();
                    if (!expandedInventories.Add(invId))
                        continue;

                    var inventoryData =
                        doc.DocumentElement.SelectNodes($"//attribute [@id='Parent'] [@value='{invId}']");
                    if (inventoryData == null) continue;

                    for (var j = 0; j < inventoryData.Count; j++)
                    {
                        var itemNode = inventoryData[j].ParentNode;
                        Item item;
                        try
                        {
                            item = ParseItem(itemNode);
                            item.ItemXmlNodeIdx = itemList.Count;
                        }
                        catch (ObjectNullException)
                        {
                            continue;
                        }
                        catch (Exception e)
                        {
                            throw new ItemParserException(e, itemNode);
                        }

                        itemList.Add(item);
                        nodeList.Add(itemNode);
                        players[i].SlotsOccupation[int.Parse(item.Slot)] = true;

                        var nested = item.NestedInventoryId;
                        if (!string.IsNullOrEmpty(nested) && nested != "0")
                            expandQueue.Enqueue(nested);
                    }
                }

                players[i].Items = itemList.ToArray();
                players[i].ItemXmlNodes = nodeList;
            });
            return players;
        }

        public static Player ParsePlayer(XmlNode node)
        {
            var player = new Player
            {
                MaxVitalityPatchCheck = node.ParentNode.ParentNode
                        .SelectSingleNode("attribute [@id='MaxVitalityPatchCheck']")?.Attributes[1].Value,
                Vitality = node.ParentNode.ParentNode
                        .SelectSingleNode("attribute [@id='Vitality']")?.Attributes[1].Value,
                InventoryId = node.ParentNode.ParentNode.SelectSingleNode("attribute [@id='Inventory']")
                        ?.Attributes[1].Value,

                Experience = node.ParentNode
                        .SelectSingleNode("node//attribute [@id='Experience']")?.Attributes[1].Value,
                Reputation = node.ParentNode
                        .SelectSingleNode("node//attribute [@id='Reputation']")?.Attributes[1].Value,

                AttributePoints =
                        node.SelectSingleNode("children//attribute [@id='AttributePoints']")
                            ?.Attributes[1].Value,
                AbilityPoints =
                        node.SelectSingleNode("children//attribute [@id='AbilityPoints']")
                            ?.Attributes[1].Value,
                TalentPoints = node.SelectSingleNode("children//attribute [@id='TalentPoints']")
                        ?.Attributes[1].Value,

                Name = node.SelectSingleNode("children//attribute [@id='Name']")?.Attributes[1].Value,
                Icon = node.SelectSingleNode("children//attribute [@id='Icon']")?.Attributes[1].Value,
                ClassType = node.SelectSingleNode("children//attribute [@id='ClassType']")?.Attributes[1]
                        .Value
            };

            var nodes = node.ParentNode.SelectNodes("node//node [@id='Skills']");
            for (var j = 0; j < nodes?.Count; j++)
            {
                player.Skills.Add(nodes[j].FirstChild.Attributes[1].Value,
                    nodes[j].ChildNodes[1].Attributes[1].Value == "True");
            }

            nodes = node.SelectNodes("children//node [@id='Attributes']");
            for (var j = 0; j < nodes?.Count; j++)
            {
                player.Attributes.Add(j, int.Parse(nodes[j].FirstChild.Attributes[1].Value));
            }

            nodes = node.SelectNodes("children//node [@id='Abilities']");
            for (var j = 0; j < nodes?.Count; j++)
            {
                player.Abilities.Add(j, int.Parse(nodes[j].FirstChild.Attributes[1].Value));
            }

            nodes = node.SelectNodes("children//node [@id='Talents']");
            for (var j = 0; j < nodes?.Count; j++)
            {
                player.Talents.Add(uint.Parse(nodes[j].FirstChild.Attributes[1].Value));
            }

            nodes = node.SelectNodes("children//node [@id='Traits']");
            for (var j = 0; j < nodes?.Count; j++)
            {
                player.Traits.Add(j, int.Parse(nodes[j].FirstChild.Attributes[1].Value));
            }

            if (player.Name == "")
                player.Name = "Henchman";

            return player;
        }

        public static Item ParseItem(XmlNode node)
        {
            var item = new Item();
#if DEBUG
                item.Xml = XmlUtilities.BeautifyXml(node);
#endif
            try
            {

                item.Flags = node.SelectSingleNode("attribute [@id='Flags']").Attributes[1].Value;
                item.IsKey = node.SelectSingleNode("attribute [@id='IsKey']").Attributes[1].Value;
                item.StatsName = node.SelectSingleNode("attribute [@id='Stats']").Attributes[1].Value;
                item.Parent = node.SelectSingleNode("attribute [@id='Parent']").Attributes[1].Value;
                item.Slot = node.SelectSingleNode("attribute [@id='Slot']").Attributes[1].Value;
                var amountAttr = node.SelectSingleNode("attribute [@id='Amount']");
                item.Amount = GetLsxAttributeValue(amountAttr) ?? amountAttr.Attributes[1].Value;
                item.IsGenerated = node.SelectSingleNode("attribute [@id='IsGenerated']").Attributes[1].Value;
                item.LockLevel = node.SelectSingleNode("attribute [@id='LockLevel']").Attributes[1].Value;
                item.Vitality = node.SelectSingleNode("attribute [@id='Vitality']").Attributes[1].Value;
                item.ItemType = node.SelectSingleNode("attribute [@id='ItemType']").Attributes[1].Value;
                item.MaxVitalityPatchCheck = node.SelectSingleNode("attribute [@id='MaxVitalityPatchCheck']").Attributes[1].Value;
                var maxDurAttr = node.SelectSingleNode("attribute [@id='MaxDurabilityPatchCheck']");
                item.MaxDurabilityPatchCheck = maxDurAttr == null
                    ? null
                    : GetLsxAttributeValue(maxDurAttr) ?? maxDurAttr.Attributes[1].Value;

                var invAttr = node.SelectSingleNode("attribute [@id='Inventory']");
                item.NestedInventoryId = invAttr?.Attributes[1].Value ?? "0";

                // Friendly label: DisplayName / Name — prefer resolved value, else Larian handle (h…;n) when only that is stored.
                var displayNameAttr = node.SelectSingleNode("attribute [@id='DisplayName']") ??
                                      node.SelectSingleNode(".//attribute [@id='DisplayName']");
                var rawDisplay = GetLsxAttributeValueOrHandle(displayNameAttr);
                item.DisplayName = string.IsNullOrWhiteSpace(rawDisplay) ? null : rawDisplay.Trim();

                if (string.IsNullOrEmpty(item.DisplayName))
                {
                    var nameAttr = node.SelectSingleNode("attribute [@id='Name']") ??
                                     node.SelectSingleNode("children//attribute [@id='Name']") ??
                                     node.SelectSingleNode(".//attribute [@id='Name']");
                    var rawName = GetLsxAttributeValueOrHandle(nameAttr);
                    item.DisplayName = string.IsNullOrWhiteSpace(rawName) ? null : rawName.Trim();
                }
            }
            catch (NullReferenceException e)
            {
                throw new ObjectNullException(e, "One or more item nodes are not found.");
            }

            // sort item
            if (item.IsKey == "True")
                item.ItemSort = ItemSortType.Key;
            else if (DataTable.GoldNames.Contains(item.StatsName.ToLower()))
                item.ItemSort = ItemSortType.Gold;
            else if (DataTable.IsContainerStatsName(item.StatsName.ToLower()))
                item.ItemSort = ItemSortType.Container;
            else
            {
                var nameParts = item.StatsName.ToLower().Split('_');

                if (nameParts[0] == "wpn" &&
                    DataTable.ArrowTypeNames.Contains(nameParts[1]))
                    item.ItemSort = ItemSortType.Arrow;
                else
                    switch (nameParts[0])
                    {
                        case "item":
                            item.ItemSort = ItemSortType.Item;
                            break;
                        case "potion":
                            item.ItemSort = ItemSortType.Potion;
                            break;
                        case "arm":
                            item.ItemSort = ItemSortType.Armor;
                            break;
                        case "wpn":
                            item.ItemSort = ItemSortType.Weapon;
                            break;
                        case "skillbook":
                            item.ItemSort = ItemSortType.Skillbook;
                            break;
                        case "scroll":
                            item.ItemSort = ItemSortType.Scroll;
                            break;
                        case "grn":
                            item.ItemSort = ItemSortType.Granade;
                            break;
                        case "food":
                            item.ItemSort = ItemSortType.Food;
                            break;
                        case "fur":
                            item.ItemSort = ItemSortType.Furniture;
                            break;
                        case "loot":
                            item.ItemSort = ItemSortType.Loot;
                            break;
                        case "quest":
                            item.ItemSort = ItemSortType.Quest;
                            break;
                        case "tool":
                            item.ItemSort = ItemSortType.Tool;
                            break;
                        case "unique":
                            item.ItemSort = ItemSortType.Unique;
                            break;
                        case "book":
                            item.ItemSort = ItemSortType.Book;
                            break;
                        default:
                            item.ItemSort = ItemSortType.Other;
                            break;
                    }
            }

            // check if has generation
            var genNode = node.SelectSingleNode("children/node [@id='Generation']");
            if (genNode != null)
            {
                item.Generation = new Item.GenerationNode
                {
                    Base = genNode.SelectSingleNode("attribute [@id='Base']").Attributes[1].Value,
                    //ItemType = genNode.SelectSingleNode("attribute [@id='ItemType']").Attributes[1].Value,
                    //Level = genNode.SelectSingleNode("attribute [@id='Level']").Attributes[1].Value,
                    Random = genNode.SelectSingleNode("attribute [@id='Random']").Attributes[1].Value
                };
                var genBoostNodes = genNode.SelectNodes("children//attribute [@id='Object']");
                item.Generation.Boosts = new List<string>();
                foreach (XmlNode n in genBoostNodes)
                {
                    item.Generation.Boosts.Add(n.Attributes[1].Value);
                    if (!GenerationBoostCollector.Contains(n.Attributes[1].Value))
                        GenerationBoostCollector.Add(n.Attributes[1].Value);
                }
            }

            // check if has stats
            var statsNode = node.SelectSingleNode("children/node [@id='Stats']");
            if (statsNode == null)
                return item;

            item.Stats = new Item.StatsNode
            {
                Durability = statsNode.SelectSingleNode("attribute [@id='Durability']").Attributes[1].Value,
                DurabilityCounter =
                    statsNode.SelectSingleNode("attribute [@id='DurabilityCounter']").Attributes[1].Value,
                RepairDurabilityPenalty = statsNode.SelectSingleNode("attribute [@id='RepairDurabilityPenalty']")
                    .Attributes[1].Value,
                Level = statsNode.SelectSingleNode("attribute [@id='Level']").Attributes[1].Value,
                Charges = statsNode.SelectSingleNode("attribute [@id='Charges']").Attributes[1].Value
            };
            var statsBoostNodes = statsNode.SelectNodes("children/node [@id='PermanentBoost']/attribute");
            item.Stats.PermanentBoost = new Dictionary<string, string>();
            foreach (XmlNode n in statsBoostNodes)
            {
                item.Stats.PermanentBoost.Add(n.Attributes[0].Value, n.Attributes[1].Value);
                if (!StatsBoostsCollector.Contains($"{n.Attributes[0].Value}\t{n.Attributes[1].Value}"))
                    StatsBoostsCollector.Add($"{n.Attributes[0].Value}\t{n.Attributes[1].Value}");
            }

            return item;
        }

        public static Meta ParseMeta(XmlDocument doc)
        {
            var metaData = doc.DocumentElement?.SelectSingleNode("./region [@id='MetaData']/node [@id='MetaData']/children/node [@id='MetaData']");
            var saveTimeNode = metaData.SelectSingleNode("children/node [@id='SaveTime']");

            if (metaData == null)
            {
                throw new XmlException("Unable to find MeteData in meta savegame.");
            }

            var meta = new Meta
            {
                Level = metaData.SelectSingleNode("attribute [@id='Level']")?.Attributes[1].Value,
                Seed = metaData.SelectSingleNode("attribute [@id='Seed']")?.Attributes[1].Value,
                Difficulty = Int16.Parse(metaData.SelectSingleNode("attribute [@id='Difficulty']")?.Attributes[1].Value),
                SavegameType = Int16.Parse(metaData.SelectSingleNode("attribute [@id='SaveGameType']")?.Attributes[1].Value),
                Year = saveTimeNode.SelectSingleNode("attribute [@id='Year']")?.Attributes[1].Value,
                Month = saveTimeNode.SelectSingleNode("attribute [@id='Month']")?.Attributes[1].Value,
                Day = saveTimeNode.SelectSingleNode("attribute [@id='Day']")?.Attributes[1].Value,
                Hours = saveTimeNode.SelectSingleNode("attribute [@id='Hours']")?.Attributes[1].Value,
                Minutes = saveTimeNode.SelectSingleNode("attribute [@id='Minutes']")?.Attributes[1].Value,
                Seconds = saveTimeNode.SelectSingleNode("attribute [@id='Seconds']")?.Attributes[1].Value,
                Milliseconds = saveTimeNode.SelectSingleNode("attribute [@id='Milliseconds']")?.Attributes[1].Value
            };

            //Debug.WriteLine("TimeStamp: " + metaData.SelectSingleNode("attribute [@id='TimeStamp']")?.Attributes[1].Value);
            //Debug.WriteLine("Size: " + metaData.SelectSingleNode("attribute [@id='Size']")?.Attributes[1].Value);
            
            var gameVersionChildrenNodes = metaData.SelectNodes("children/node [@id='GameVersions']/children/node [@id='GameVersion']");
            foreach (XmlNode gameVersionNode in gameVersionChildrenNodes)
            {
                meta.GameVersions.Add(gameVersionNode.SelectSingleNode("attribute")?.Attributes[1].Value);
            }

            var modsChildrenNodes = metaData.SelectNodes("children/node [@id='ModuleSettings']/children/node [@id='Mods']/children/node [@id='ModuleShortDesc']");
            foreach (XmlNode moduleModNode in modsChildrenNodes)
            {
                meta.ModNames.Add(moduleModNode.SelectSingleNode("attribute [@id='Name']")?.Attributes[1].Value);
            }

            return meta;
        }
        #endregion

        #region write file
        public static XmlDocument WritePlayer(XmlDocument doc, Player[] players)
        {
            // find player data
            var playerData = doc.DocumentElement.SelectNodes("//*[@id='PlayerData']");

            if (playerData == null)
                throw new XmlException("Unable to find any player data in the savegame.");

            // Sequential: XmlDocument is not safe for concurrent writes; item edits must target this loaded doc.
            for (var i = 0; i < playerData.Count; i++)
            {
                playerData[i].ParentNode.ParentNode.SelectSingleNode("attribute [@id='MaxVitalityPatchCheck']")
                    .Attributes[1].Value = players[i].MaxVitalityPatchCheck;
                playerData[i].ParentNode.ParentNode.SelectSingleNode("attribute [@id='Vitality']").Attributes[1].Value =
                    players[i].Vitality;
                playerData[i].ParentNode.ParentNode.SelectSingleNode("attribute [@id='Inventory']").Attributes[1].Value
                    = players[i].InventoryId;
                playerData[i].ParentNode.SelectSingleNode("node//attribute [@id='Experience']").Attributes[1].Value =
                    players[i].Experience;
                playerData[i].ParentNode.SelectSingleNode("node//attribute [@id='Reputation']").Attributes[1].Value =
                    players[i].Reputation;

                playerData[i].SelectSingleNode("children//attribute [@id='AttributePoints']").Attributes[1].Value =
                    players[i].AttributePoints;
                playerData[i].SelectSingleNode("children//attribute [@id='AbilityPoints']").Attributes[1].Value =
                    players[i].AbilityPoints;
                playerData[i].SelectSingleNode("children//attribute [@id='TalentPoints']").Attributes[1].Value =
                    players[i].TalentPoints;


                //var nodes = playerData[i].ParentNode.SelectNodes("node//node [@id='Skills']");
                //for (var j = 0; j < nodes?.Count; j++)
                //{
                //    Players[i].Skills.Add(nodes[j].FirstChild.Attributes[1].Value, skills[j].ChildNodes[1].Attributes[1].Value == "True");
                //}

                var nodes = playerData[i].SelectNodes("children//node [@id='Attributes']");
                for (var j = 0; j < nodes?.Count; j++)
                {
                    nodes[j].FirstChild.Attributes[1].Value = players[i].Attributes[j].ToString();
                }

                nodes = playerData[i].SelectNodes("children//node [@id='Abilities']");
                for (var j = 0; j < nodes?.Count; j++)
                {
                    nodes[j].FirstChild.Attributes[1].Value = players[i].Abilities[j].ToString();
                }

                nodes = playerData[i].SelectNodes("children//node [@id='Talents']");
                for (var j = 0; j < nodes?.Count; j++)
                {
                    nodes[j].FirstChild.Attributes[1].Value = players[i].Talents[j].ToString();
                }

                nodes = playerData[i].SelectNodes("children//node [@id='Traits']");
                for (var j = 0; j < nodes?.Count; j++)
                {
                    nodes[j].FirstChild.Attributes[1].Value = players[i].Traits[j].ToString();
                }

                // write item changes
                doc = WriteItemChanges(doc, players[i]);
            }

            return doc;
        }

        /// <summary>
        /// Replays the same BFS inventory walk as <see cref="ParsePlayer"/> so we get live <see cref="XmlNode"/>
        /// references into <paramref name="doc"/>. Required for save: <see cref="Savegame.WriteEditsToLsxAsync"/>
        /// loads a new <see cref="XmlDocument"/>; cached <see cref="Player.ItemXmlNodes"/> point at the old tree.
        /// </summary>
        private static List<XmlNode> CollectItemXmlNodesForPlayer(XmlDocument doc, Player player)
        {
            var nodeList = new List<XmlNode>();
            var expandQueue = new Queue<string>();
            var expandedInventories = new HashSet<string>();
            expandQueue.Enqueue(player.InventoryId);

            while (expandQueue.Count > 0)
            {
                var invId = expandQueue.Dequeue();
                if (!expandedInventories.Add(invId))
                    continue;

                var inventoryData =
                    doc.DocumentElement.SelectNodes($"//attribute [@id='Parent'] [@value='{invId}']");
                if (inventoryData == null) continue;

                for (var j = 0; j < inventoryData.Count; j++)
                {
                    var itemNode = inventoryData[j].ParentNode;
                    Item item;
                    try
                    {
                        item = ParseItem(itemNode);
                    }
                    catch (ObjectNullException)
                    {
                        continue;
                    }
                    catch (Exception e)
                    {
                        throw new ItemParserException(e, itemNode);
                    }

                    nodeList.Add(itemNode);

                    var nested = item.NestedInventoryId;
                    if (!string.IsNullOrEmpty(nested) && nested != "0")
                        expandQueue.Enqueue(nested);
                }
            }

            return nodeList;
        }

        public static XmlDocument WriteItemChanges(XmlDocument doc, Player player)
        {
            if (player.ItemChanges == null || player.ItemChanges.Count == 0)
                return doc;

            if (player.Items == null)
                throw new InvalidOperationException("Player.Items is null.");

            var itemNodes = CollectItemXmlNodesForPlayer(doc, player);
            if (itemNodes.Count != player.Items.Length)
                throw new InvalidOperationException(
                    $"Item XML node count ({itemNodes.Count}) must match Player.Items length ({player.Items.Length}) when writing inventory.");

            foreach (var ic in player.ItemChanges)
            {
                try
                {
                    if (ic.Value.ChangeType == ChangeType.Add)
                    {

                    }
                    else if (ic.Value.ChangeType == ChangeType.Delete)
                    {

                    }
                    else if (ic.Value.ChangeType == ChangeType.Modify)
                    {
                        var itemNode = itemNodes[ic.Value.Item.ItemXmlNodeIdx];

                        var allowedChanges = ic.Value.Item.GetAllowedChangeType();
                        if (allowedChanges.Contains(nameof(ic.Value.Item.Amount)))
                            SetLsxAttributeValue(itemNode, "Amount", ic.Value.Item.Amount);

                        if (allowedChanges.Contains(nameof(ic.Value.Item.LockLevel)))
                            SetLsxAttributeValue(itemNode, "LockLevel", ic.Value.Item.LockLevel);

                        if (allowedChanges.Contains(nameof(ic.Value.Item.Vitality)))
                        {
                            SetLsxAttributeValue(itemNode, "Vitality", ic.Value.Item.Vitality);
                            SetLsxAttributeValue(itemNode, "MaxVitalityPatchCheck", ic.Value.Item.MaxVitalityPatchCheck);
                        }

                        if (allowedChanges.Contains(nameof(ic.Value.Item.ItemRarity)))
                            SetLsxAttributeValue(itemNode, "ItemType", ic.Value.Item.ItemRarity.ToString());

                        // MaxDurabilityPatchCheck: display-only — game derives real max from item data at runtime.

                        // check if has generation
                        if (allowedChanges.Contains(nameof(ic.Value.Item.Generation)) &&
                            ic.Value.Item.Generation != null)
                        {
                            var genNode = itemNode.SelectSingleNode("children/node [@id='Generation']");
                            if (genNode == null)
                            {
                                // create a generation node
                                genNode = doc.CreateDocumentFragment();
                                // TODO Level is taken from stats.level
                                var level = ic.Value.Item.Stats == null ? "1" : ic.Value.Item.Stats.Level;
                                // ItemType is taken from item.ItemType
                                genNode.InnerXml =
                                    $"<node id=\"Generation\"><attribute id=\"Base\" value=\"{ic.Value.Item.Generation.Base}\" type=\"22\" /><attribute id=\"ItemType\" value=\"{ic.Value.Item.ItemType}\" type=\"22\" /><attribute id=\"Level\" value=\"{level}\" type=\"2\" /><attribute id=\"Random\" value=\"{ic.Value.Item.Generation.Random}\" type=\"4\" /><children /></node>";
                                // insert after max durability node
                                itemNode.SelectSingleNode("children").AppendChild(genNode);
                                genNode = itemNode.SelectSingleNode("children/node [@id='Generation']");
                            }

                            var childrenNode = genNode.SelectSingleNode("children");
                            if (childrenNode == null)
                            {
                                childrenNode = doc.CreateElement("children");
                                genNode.AppendChild(childrenNode);
                                childrenNode = genNode.SelectSingleNode("children");
                            }

                            // wipe all existing boost
                            childrenNode.RemoveAll();
                            // then add each defined boosts
                            foreach (var boostName in ic.Value.Item.Generation.Boosts)
                            {
                                var boost = doc.CreateDocumentFragment();
                                boost.InnerXml =
                                    $"<node id=\"Boost\"><attribute id=\"Object\" value=\"{boostName}\" type=\"22\" /></node>";
                                childrenNode.AppendChild(boost);
                            }

                            SetLsxAttributeValue(itemNode, "IsGenerated", "True");
                        }

                        // check if has stats
                        var statsNode = itemNode.SelectSingleNode("children/node [@id='Stats']");
                        if (!allowedChanges.Contains(nameof(ic.Value.Item.Stats)) || statsNode == null ||
                            ic.Value.Item.Stats == null)
                            continue;

                        SetLsxAttributeValue(statsNode, "Durability", ic.Value.Item.Stats.Durability);
                        SetLsxAttributeValue(statsNode, "DurabilityCounter", ic.Value.Item.Stats.DurabilityCounter);
                        SetLsxAttributeValue(statsNode, "RepairDurabilityPenalty", ic.Value.Item.Stats.RepairDurabilityPenalty);
                        SetLsxAttributeValue(statsNode, "Level", ic.Value.Item.Stats.Level);
                        SetLsxAttributeValue(statsNode, "ItemType", ic.Value.Item.ItemType);
                        SetLsxAttributeValue(statsNode, "IsIdentified", "1");
                    }

                }
                catch (Exception ex)
                {
                    throw new ObjectNullException(ex, ic.Value);
                }
            }

            return doc;
        }
        #endregion
    }


}