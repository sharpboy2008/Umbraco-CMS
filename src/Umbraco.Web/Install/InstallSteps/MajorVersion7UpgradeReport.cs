using System;
using System.Collections.Generic;
using System.Linq;
using NPoco;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Persistence.SqlSyntax;
using Umbraco.Core.PropertyEditors;
using Umbraco.Web.Install.Models;

namespace Umbraco.Web.Install.InstallSteps
{
    [InstallSetupStep(InstallationType.Upgrade,
        "MajorVersion7UpgradeReport", 1, "")]
    internal class MajorVersion7UpgradeReport : InstallSetupStep<object>
    {
        private readonly DatabaseContext _databaseContext;
        private readonly IRuntimeState _runtime;

        public MajorVersion7UpgradeReport(DatabaseContext databaseContext, IRuntimeState runtime)
        {
            _databaseContext = databaseContext;
            _runtime = runtime;
        }

        public override InstallSetupResult Execute(object model)
        {
            //we cannot run this step if the db is not configured.
            if (_databaseContext.IsDatabaseConfigured == false)
            {
                return null;
            }

            var result = _databaseContext.ValidateDatabaseSchema();
            var determinedVersion = result.DetermineInstalledVersion();

            return new InstallSetupResult("version7upgradereport", 
                new
                {
                    currentVersion = determinedVersion.ToString(),
                    newVersion = UmbracoVersion.Current.ToString(),
                    errors = CreateReport()
                });
        }

        public override bool RequiresExecution(object model)
        {
            //if it's configured, then no need to run
            if (_runtime.Level == RuntimeLevel.Run)
                return false;

            try
            {
                //we cannot run this step if the db is not configured.
                if (_databaseContext.IsDatabaseConfigured == false)
                {
                    return false;
                }
            }
            catch (InvalidOperationException)
            {
                //if there is no db context
                return false;
            }

            var result = _databaseContext.ValidateDatabaseSchema();
            var determinedVersion = result.DetermineInstalledVersion();
            if ((string.IsNullOrWhiteSpace(GlobalSettings.ConfigurationStatus) == false || determinedVersion.Equals(new Version(0, 0, 0)) == false)
                && UmbracoVersion.Current.Major > determinedVersion.Major)
            {
                //it's an upgrade to a major version so we're gonna show this step if there are issues

                var report = CreateReport();
                return report.Any();
            }

            return false;
        }

        private IEnumerable<string> CreateReport()
        {
            var errorReport = new List<string>();

            var sqlSyntax = _databaseContext.SqlSyntax;

            var sql = new Sql();
            sql
                .Select(
                    sqlSyntax.GetQuotedColumn("cmsDataType", "controlId"),
                    sqlSyntax.GetQuotedColumn("umbracoNode", "text"))
                .From(sqlSyntax.GetQuotedTableName("cmsDataType"))
                .InnerJoin(sqlSyntax.GetQuotedTableName("umbracoNode"))
                .On(
                    sqlSyntax.GetQuotedColumn("cmsDataType", "nodeId") + " = " +
                    sqlSyntax.GetQuotedColumn("umbracoNode", "id"));

            var list = _databaseContext.Database.Fetch<dynamic>(sql);
            foreach (var item in list)
            {
                Guid legacyId = item.controlId;
                //check for a map entry
                var alias = LegacyPropertyEditorIdToAliasConverter.GetAliasFromLegacyId(legacyId);
                if (alias != null)
                {
                    //check that the new property editor exists with that alias
                    var editor = Current.PropertyEditors[alias];
                    if (editor == null)
                    {
                        errorReport.Add(string.Format("Property Editor with ID '{0}' (assigned to Data Type '{1}') has a valid GUID -> Alias map but no property editor was found. It will be replaced with a Readonly/Label property editor.", item.controlId, item.text));
                    }
                }
                else
                {
                    errorReport.Add(string.Format("Property Editor with ID '{0}' (assigned to Data Type '{1}') does not have a valid GUID -> Alias map. It will be replaced with a Readonly/Label property editor.", item.controlId, item.text));
                }
            }

            return errorReport;
        }
    }
}