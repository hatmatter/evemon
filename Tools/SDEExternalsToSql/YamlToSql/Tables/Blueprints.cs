﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace EVEMon.SDEExternalsToSql.YamlToSql.Tables
{
    internal enum Activity
    {
        None = 0,
        Manufacturing = 1,
        ResearchingTechnology = 2,
        ResearchingTimeProductivity = 3,
        ResearchingMaterialProductivity = 4,
        Copying = 5,
        Duplicating = 6,
        ReverseEngineering = 7,
        Invention = 8
    }

    internal static class Blueprints
    {
        private const string InvBlueprintTypesTableName = "invBlueprintTypes";
        private const string RamTypeRequirementsTableName = "ramTypeRequirements";

        // blueprints.yaml
        private const string ActivitiesText = "activities";
        private const string BlueprintTypeIDText = "blueprintTypeID";
        private const string MaxProductionLimitText = "maxProductionLimit";
        private const string MaterialsText = "materials";
        private const string ProductsText = "products";
        private const string SkillsText = "skills";
        private const string TimeText = "time";
        private const string QuantityText = "quantity";
        private const string ProbabilityText = "probability";
        private const string RaceIDText = "raceID";
        private const string LevelText = "level";
        private const string ConsumeText = "consume";

        // invBlueprintTypes
        private const string IbtBlueprintTypeIDText = "blueprintTypeID";
        private const string ProductTypeIDText = "productTypeID";
        private const string ProductionTimeText = "productionTime";
        private const string ResearchProductivityTimeText = "researchProductivityTime";
        private const string ResearchMaterialTimeText = "researchMaterialTime";
        private const string ResearchCopyTimeText = "researchCopyTime";
        private const string ResearchTechTimeText = "researchTechTime";
        private const string DuplicatingTimeText = "duplicatingTime";
        private const string ReverseEngineeringTimeText = "reverseEngineeringTime";
        private const string InventionTimeText = "inventionTime";
        private const string IbtMaxProductionLimitText = "maxProductionLimit";

        // ramTypeRequirements
        private const string TypeIDText = "typeID";
        private const string ActivityIDText = "activityID";
        private const string RequiredTypeIDText = "requiredTypeID";
        private const string RtrQuantityText = "quantity";
        private const string RtrLevelText = "level";
        private const string RtrRaceIDText = "raceID";
        private const string RtrProbabilityText = "probability";
        private const string RtrConsumeText = "consume";


        public static void Import()
        {
            DateTime startTime = DateTime.Now;
            Util.ResetCounters();

            var yamlFile = YamlFilesConstants.blueprints;
            var filePath = Util.CheckYamlFileExists(yamlFile);

            if (String.IsNullOrEmpty(filePath))
                return;

            YamlMappingNode rNode = Util.ParseYamlFile(filePath);

            if (rNode == null)
            {
                Console.WriteLine(@"Unable to parse {0}.", yamlFile);
                return;
            }

            Console.WriteLine();
            Console.Write(@"Importing {0}... ", yamlFile);

            Database.CreateTable(InvBlueprintTypesTableName);
            Database.CreateTable(RamTypeRequirementsTableName);

            ImportData(rNode);

            Util.DisplayEndTime(startTime);

            Console.WriteLine();
        }

        private static void ImportData(YamlMappingNode rNode)
        {
            using (SqlTransaction tx = Database.SqlConnection.BeginTransaction())
            {
                IDbCommand command = new SqlCommand { Connection = Database.SqlConnection, Transaction = tx };
                try
                {
                    YamlNode manActivity = new YamlScalarNode(((int)Activity.Manufacturing).ToString(CultureInfo.InvariantCulture));
                    YamlNode rteActivity =
                        new YamlScalarNode(((int)Activity.ResearchingTechnology).ToString(CultureInfo.InvariantCulture));
                    YamlNode rtpActivity =
                        new YamlScalarNode(((int)Activity.ResearchingTimeProductivity).ToString(CultureInfo.InvariantCulture));
                    YamlNode rmpActivity =
                        new YamlScalarNode(((int)Activity.ResearchingMaterialProductivity).ToString(CultureInfo.InvariantCulture));
                    YamlNode copActivity = new YamlScalarNode(((int)Activity.Copying).ToString(CultureInfo.InvariantCulture));
                    YamlNode dupActivity = new YamlScalarNode(((int)Activity.Duplicating).ToString(CultureInfo.InvariantCulture));
                    YamlNode renActivity =
                        new YamlScalarNode(((int)Activity.ReverseEngineering).ToString(CultureInfo.InvariantCulture));
                    YamlNode invActivity = new YamlScalarNode(((int)Activity.Invention).ToString(CultureInfo.InvariantCulture));

                    foreach (KeyValuePair<YamlNode, YamlNode> pair in rNode.Children)
                    {
                        Util.UpdatePercentDone(rNode.Count());

                        String productTypeIDText = Database.Null;
                        String productionTimeText = Database.Null;
                        String researchTechTimeText = Database.Null;
                        String researchProductivityTimeText = Database.Null;
                        String researchMaterialTimeText = Database.Null;
                        String researchCopyTimeText = Database.Null;
                        String duplicatingTimeText = Database.Null;
                        String reverseEngeneeringTimeText = Database.Null;
                        String inventionTimeText = Database.Null;

                        YamlMappingNode cNode = rNode.Children[pair.Key] as YamlMappingNode;

                        if (cNode == null)
                            continue;

                        String blueprintTypeIDText = pair.Key.ToString();
                        YamlNode blueprintTypeIDNode = cNode.Children[new YamlScalarNode(BlueprintTypeIDText)];
                        if (blueprintTypeIDText != blueprintTypeIDNode.ToString())
                            throw new Exception("Key differs from " + BlueprintTypeIDText);

                        YamlNode activitiesNode = new YamlScalarNode(ActivitiesText);
                        if (cNode.Children.ContainsKey(activitiesNode))
                        {
                            YamlMappingNode activityNode = cNode.Children[activitiesNode] as YamlMappingNode;

                            if (activityNode == null)
                                continue;

                            foreach (KeyValuePair<YamlNode, YamlNode> activity in activityNode)
                            {
                                YamlMappingNode actNode = activity.Value as YamlMappingNode;

                                if (actNode == null)
                                    continue;

                                if (activity.Key.Equals(manActivity))
                                {
                                    if (actNode.Children.Keys.Any(key => key.ToString() == ProductsText))
                                        productTypeIDText =
                                            ((YamlMappingNode)actNode.Children[new YamlScalarNode(ProductsText)]).Children.First()
                                                .Key.ToString();

                                    if (actNode.Children.Keys.Any(key => key.ToString() == TimeText))
                                        productionTimeText = actNode.Children[new YamlScalarNode(TimeText)].ToString();
                                }

                                if (activity.Key.Equals(rteActivity))
                                {
                                    if (actNode.Children.Keys.Any(key => key.ToString() == TimeText))
                                        researchTechTimeText = actNode.Children[new YamlScalarNode(TimeText)].ToString();
                                }

                                if (activity.Key.Equals(rtpActivity))
                                {
                                    if (actNode.Children.Keys.Any(key => key.ToString() == TimeText))
                                        researchProductivityTimeText = actNode.Children[new YamlScalarNode(TimeText)].ToString();
                                }

                                if (activity.Key.Equals(rmpActivity))
                                {
                                    if (actNode.Children.Keys.Any(key => key.ToString() == TimeText))
                                        researchMaterialTimeText = actNode.Children[new YamlScalarNode(TimeText)].ToString();
                                }

                                if (activity.Key.Equals(copActivity))
                                {
                                    if (actNode.Children.Keys.Any(key => key.ToString() == TimeText))
                                        researchCopyTimeText = actNode.Children[new YamlScalarNode(TimeText)].ToString();
                                }

                                if (activity.Key.Equals(dupActivity))
                                {
                                    if (actNode.Children.Keys.Any(key => key.ToString() == TimeText))
                                        duplicatingTimeText = actNode.Children[new YamlScalarNode(TimeText)].ToString();
                                }

                                if (activity.Key.Equals(renActivity))
                                {
                                    if (actNode.Children.Keys.Any(key => key.ToString() == TimeText))
                                        reverseEngeneeringTimeText = actNode.Children[new YamlScalarNode(TimeText)].ToString();
                                }

                                if (activity.Key.Equals(invActivity))
                                {
                                    if (actNode.Children.Keys.Any(key => key.ToString() == TimeText))
                                        inventionTimeText = actNode.Children[new YamlScalarNode(TimeText)].ToString();
                                }

                                if (!activity.Key.Equals(manActivity))
                                    ImportProducts(command, activity, blueprintTypeIDText);

                                ImportMaterials(command, activity, blueprintTypeIDText);
                                ImportSkills(command, activity, blueprintTypeIDText);
                            }
                        }

                        Dictionary<string, string> parameters = new Dictionary<string, string>();
                        parameters[IbtBlueprintTypeIDText] = blueprintTypeIDText;
                        parameters[ProductTypeIDText] = productTypeIDText;
                        parameters[ProductionTimeText] = productionTimeText;
                        parameters[ResearchProductivityTimeText] = researchProductivityTimeText;
                        parameters[ResearchMaterialTimeText] = researchMaterialTimeText;
                        parameters[ResearchCopyTimeText] = researchCopyTimeText;
                        parameters[ResearchTechTimeText] = researchTechTimeText;
                        parameters[DuplicatingTimeText] = duplicatingTimeText;
                        parameters[ReverseEngineeringTimeText] = reverseEngeneeringTimeText;
                        parameters[InventionTimeText] = inventionTimeText;
                        parameters[IbtMaxProductionLimitText] =
                            cNode.Children.Keys.Any(key => key.ToString() == MaxProductionLimitText)
                                ? cNode.Children[new YamlScalarNode(MaxProductionLimitText)].ToString()
                                : Database.Null;

                        command.CommandText = Database.SqlInsertCommandText(InvBlueprintTypesTableName, parameters);
                        command.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
                catch (SqlException e)
                {
                    tx.Rollback();
                    Util.HandleException(command, e);
                }
            }
        }

        private static void ImportProducts(IDbCommand command, KeyValuePair<YamlNode, YamlNode> activity,
            String blueprintTypeIDText)
        {
            if (String.IsNullOrWhiteSpace(blueprintTypeIDText) || blueprintTypeIDText == Database.Null)
                return;

            YamlMappingNode actNode = activity.Value as YamlMappingNode;

            if (actNode == null)
                return;

            YamlNode productsNode = new YamlScalarNode(ProductsText);
            if (!actNode.Children.ContainsKey(productsNode))
                return;

            YamlMappingNode prodsNode = actNode.Children[productsNode] as YamlMappingNode;

            if (prodsNode == null)
                return;

            foreach (KeyValuePair<YamlNode, YamlNode> product in prodsNode)
            {
                YamlMappingNode prodNode = product.Value as YamlMappingNode;

                if (prodNode == null)
                    continue;

                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters[TypeIDText] = blueprintTypeIDText;
                parameters[ActivityIDText] = activity.Key.ToString();
                parameters[RequiredTypeIDText] = product.Key.ToString();
                parameters[RtrQuantityText] = prodNode.Children.Keys.Any(key => key.ToString() == QuantityText)
                    ? prodNode.Children[new YamlScalarNode(QuantityText)].ToString()
                    : Database.Null;
                parameters[RtrProbabilityText] = prodNode.Children.Keys.Any(key => key.ToString() == ProbabilityText)
                    ? prodNode.Children[new YamlScalarNode(ProbabilityText)].ToString()
                    : Database.Null;
                parameters[RtrRaceIDText] = prodNode.Children.Keys.Any(key => key.ToString() == RaceIDText)
                    ? prodNode.Children[new YamlScalarNode(RaceIDText)].ToString()
                    : Database.Null;

                command.CommandText = Database.SqlInsertCommandText(RamTypeRequirementsTableName, parameters);
                command.ExecuteNonQuery();
            }
        }

        private static void ImportMaterials(IDbCommand command, KeyValuePair<YamlNode, YamlNode> activity,
            String productTypeIDText)
        {
            if (String.IsNullOrWhiteSpace(productTypeIDText) || productTypeIDText == Database.Null)
                return;

            YamlMappingNode actNode = activity.Value as YamlMappingNode;

            if (actNode == null)
                return;

            YamlNode materialsNode = new YamlScalarNode(MaterialsText);
            if (!actNode.Children.ContainsKey(materialsNode))
                return;

            YamlMappingNode matsNode = actNode.Children[materialsNode] as YamlMappingNode;

            if (matsNode == null)
                return;

            foreach (KeyValuePair<YamlNode, YamlNode> material in matsNode)
            {
                YamlMappingNode matNode = material.Value as YamlMappingNode;

                if (matNode == null)
                    continue;

                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters[TypeIDText] = productTypeIDText;
                parameters[ActivityIDText] = activity.Key.ToString();
                parameters[RequiredTypeIDText] = material.Key.ToString();
                parameters[RtrQuantityText] = matNode.Children.Keys.Any(key => key.ToString() == QuantityText)
                    ? matNode.Children[new YamlScalarNode(QuantityText)].ToString()
                    : Database.Null;
                parameters[RtrConsumeText] = matNode.Children.Keys.Any(key => key.ToString() == ConsumeText)
                    ? Convert.ToByte(Convert.ToBoolean(matNode.Children[new YamlScalarNode(ConsumeText)].ToString()))
                        .ToString(CultureInfo.InvariantCulture)
                    : Database.Null;

                command.CommandText = Database.SqlInsertCommandText(RamTypeRequirementsTableName, parameters);
                command.ExecuteNonQuery();
            }
        }

        private static void ImportSkills(IDbCommand command, KeyValuePair<YamlNode, YamlNode> activity, String productTypeIDText)
        {
            if (String.IsNullOrWhiteSpace(productTypeIDText) || productTypeIDText == Database.Null)
                return;

            YamlMappingNode actNode = activity.Value as YamlMappingNode;

            if (actNode == null)
                return;

            YamlNode skillsNode = new YamlScalarNode(SkillsText);
            if (!actNode.Children.ContainsKey(skillsNode))
                return;

            YamlMappingNode sksNode = actNode.Children[skillsNode] as YamlMappingNode;

            if (sksNode == null)
                return;

            foreach (KeyValuePair<YamlNode, YamlNode> skill in sksNode)
            {
                YamlMappingNode skillNode = skill.Value as YamlMappingNode;

                if (skillNode == null)
                    continue;

                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters[TypeIDText] = productTypeIDText;
                parameters[ActivityIDText] = activity.Key.ToString();
                parameters[RequiredTypeIDText] = skill.Key.ToString();
                parameters[RtrLevelText] = skillNode.Children.Keys.Any(key => key.ToString() == LevelText)
                    ? skillNode.Children[new YamlScalarNode(LevelText)].ToString()
                    : Database.Null;

                command.CommandText = Database.SqlInsertCommandText(RamTypeRequirementsTableName, parameters);
                command.ExecuteNonQuery();
            }
        }
    }
}