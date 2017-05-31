using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization.ObjectModel;
using Sitecore.Diagnostics;
using Sitecore.Update;
using Sitecore.Update.Commands;
using Sitecore.Update.Installer;
using Sitecore.Update.Installer.Items;
using Sitecore.Update.Installer.Utils;
using Sitecore.Install.Framework;
using Sitecore.Data;
using Sitecore.Configuration;

namespace Sitecore.Support.Update.Installer.Items
{
  public class AddItemCommandInstaller : Sitecore.Update.Installer.Items.AddItemCommandInstaller
  {
        private static List<ID> LayoutPathIds;
        static AddItemCommandInstaller()
        {
            Type AddItemCommandInstallerType = typeof(Sitecore.Update.Installer.Items.AddItemCommandInstaller);
            FieldInfo LayoutPathIdsField = AddItemCommandInstallerType.GetField("LayoutPathIds", BindingFlags.NonPublic | BindingFlags.Static);
            LayoutPathIds = (System.Collections.Generic.List<ID>)LayoutPathIdsField.GetValue(null);
        }

        

        protected override Sitecore.Update.Installer.Items.AddItemCommandInstaller.ItemInstaller GetGeneralItemInstaller(AddItemCommand command, string commandKey)
        {
            Assert.ArgumentNotNull(command, "command");
            Assert.ArgumentNotNull(commandKey, "commandKey");
            return new ItemInstaller(commandKey, command, new LogMethod(this.Log));
        }

        protected class ItemInstaller : Sitecore.Update.Installer.Items.AddItemCommandInstaller.ItemInstaller
        {

            public ItemInstaller(string commandKey, AddItemCommand command, AddItemCommandInstaller.LogMethod loggerMethod)
                : base(commandKey, command, loggerMethod)
            {
            }

            protected override void UpdateSharedFields(string addCommandKey, Item sitecoreItem, SyncItem item, CommandInstallerContext context)
            {
                Assert.ArgumentNotNull(addCommandKey, "addCommandKey");
                Assert.ArgumentNotNull(item, "item");
                Assert.ArgumentNotNull(context, "context");
                if (sitecoreItem != null)
                {
                    foreach (SyncField field in item.SharedFields)
                    {
                        ChangeEntry entry;
                        ItemFieldChangedProcessor fieldProcessor = this.GetFieldProcessor();
                        //added one more condition (sitecoreItem.ID.ToString()!= item.ID.ToString()) to make update of shared field according to inforequest 149259
                        if (string.IsNullOrEmpty(field.FieldValue) && InstallerUtils.IsStandardValueItem(item, sitecoreItem.Database)&&(sitecoreItem.ID.ToString()!= item.ID.ToString()))
                        {
                            Field field3 = sitecoreItem.Fields[field.FieldID];
                            string newValue = string.Empty;
                            if (field3 != null)
                            {
                                newValue = Sitecore.Update.Utils.ItemUtils.GetFieldValue(field3);
                            }
                            entry = new ChangeEntry("fieldvalue", string.Empty, newValue);
                        }
                        else
                        {
                            entry = new ChangeEntry("fieldvalue", string.Empty, field.FieldValue);
                        }
                        Field field2 = sitecoreItem.Fields[field.FieldID];
                        if (field2 != null)
                        {
                            if (AddItemCommandInstaller.LayoutPathIds.Contains(field2.ID))
                            {
                                string NewValue = entry.NewValue;
                                //new instanse is created to avoid reflection entry.OldValue is internal field, the initial code is below
                                //entry.OldValue = Sitecore.Update.Utils.ItemUtils.GetFieldValue(field2) ?? string.Empty;
                                entry = new ChangeEntry("fieldvalue", Sitecore.Update.Utils.ItemUtils.GetFieldValue(field2) ?? string.Empty, NewValue);
                                
                            }
                        fieldProcessor.Process(string.Concat(new object[] { addCommandKey, "_", fieldProcessor, "_", field.FieldID }), sitecoreItem, field2, entry, null, context);
                        }
                    }
                }
            }


            protected override CollisionBehavior InstallLightweightItem(PackageEntry entry, SyncItem item, AddItemCommand addCommand, CommandInstallerContext context, string addCommandKey, out Item sitecoreItem)
            {
                CollisionBehavior force;
                Assert.ArgumentNotNull(entry, "entry");
                Assert.ArgumentNotNull(item, "item");
                Assert.ArgumentNotNull(addCommand, "addCommand");
                Assert.ArgumentNotNull(context, "context");
                Assert.ArgumentNotNull(addCommandKey, "addCommandKey");
                Database database = Factory.GetDatabase(item.DatabaseName);
                Assert.IsNotNull(database, "database");
                sitecoreItem = this.GetItem(ID.Parse(item.ID), database, context);
                if ((sitecoreItem == null) && InstallerUtils.IsStandardValueItem(item, database))
                {
                    Item innerItem = (Item)this.GetTemplateItem(ID.Parse(item.TemplateID), database, context);
                    if (innerItem != null)
                    {
                        sitecoreItem = new TemplateItem(innerItem).StandardValues;
                        if (sitecoreItem != null)
                        {
                            context.ItemsNotToDelete.Add(sitecoreItem.ID);
                        }
                    }
                }
                if (sitecoreItem == null)
                {
                    force = CollisionBehavior.Force;
                    sitecoreItem = this.CreateLightweightItem(CollisionBehavior.Force, null, item, database, addCommand, context);
                    return force;
                }
                this.IsNewItem = false;
                string prefix = InstallerUtils.GetPrefix(sitecoreItem.ID.ToString(), item.ID);
                force = context.GetBehavior(addCommandKey, addCommand, prefix, entry);
                if (InstallerUtils.IsStandardValueItem(item, database))
                {
                    force = CollisionBehavior.Force;
                }
                if (string.Compare(sitecoreItem.ID.ToString(), item.ID, true) != 0)
                {
                    //commented the line below. this line made sitecore to process one scenario: if existent item and item to install have the same IDs
                    //item.ID = sitecoreItem.ID.ToString();
                }
                sitecoreItem = this.CreateLightweightItem(force, sitecoreItem, item, database, addCommand, context);
                return force;
            }


            protected override void InstallVersion(string addCommandKey, SyncVersion version, SyncItem item, Item newItem, CommandInstallerContext context)
            {
                Assert.ArgumentNotNull(addCommandKey, "addCommandKey");
                Assert.ArgumentNotNull(version, "version");
                Assert.ArgumentNotNull(item, "item");
                Assert.ArgumentNotNull(newItem, "newItem");
                Assert.ArgumentNotNull(context, "context");
                if (!InstallerUtils.IsStandardValueItem(item, newItem.Database))
                {
                    new ItemVersionAddedProcessor().InstallVersion(version, item.ID, item.Name, item.MasterID, newItem);
                }
                else
                {
                    //initialy newItem.ID has been passed to GetItem. in this case sitecoreItem is always != null
                    Item sitecoreItem = newItem.Database.GetItem(item.ID, Globalization.Language.Parse(version.Language), Sitecore.Data.Version.Parse(version.Version));
                    //replaced action
                    if ((sitecoreItem != null) && (sitecoreItem.Versions.Count > 0))
                    {
                        new ItemVersionAddedProcessor().InstallVersion(version, item.ID, item.Name, item.MasterID, newItem);
                    }
                    else
                    {
                        new ItemVersionAddedProcessor().MergeVersion(string.Format("{0}_{1}_{2}", addCommandKey, version.Language, version.Version), item, version, sitecoreItem, context);
                        
                    }
                }
            }
        }
    }
}
