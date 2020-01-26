using System;
using System.Collections.Generic;
using System.Configuration;

using Microsoft.SharePoint.Administration.Claims;
using Microsoft.SharePoint.WebControls;

namespace NS.Extranet.ADFSClaimsProvider
{
    public class NSADFSClaimsProvider : SPClaimProvider
    {
        #region Constructor
        public NSADFSClaimsProvider(string displayName)
            : base(displayName)
        {
        }
        #endregion


        #region Properties
        internal static string ProviderInternalName
        {
            get { return "NSADFSClaimsProvider"; }
        }

        public override string Name
        {
            get { return ProviderInternalName; }
        }

        internal static string ProviderDisplayName
        {
            get { return "NS Employees Claim Provider"; }
        }

        private static string LDAPClaimType
        {
            get { return "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"; }
            //get { return "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"; }
            //get { return "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"; }
        }
        private static string LDAPClaimValueType
        {
            get { return Microsoft.IdentityModel.Claims.ClaimValueTypes.String; }
        }

        internal static string SPTrustedIdentityTokenIssuerName
        {
            get
            {
                return ConfigurationManager.AppSettings["SPTrustedIdentityTokenIssuerName"];

            }
        }




        public override bool SupportsEntityInformation
        {
            //Not doing claims augmentation
            get { return false; }
        }

        public override bool SupportsHierarchy
        {
            get { return false; }
        }

        public override bool SupportsResolve
        {
            get { return true; }
        }

        public override bool SupportsSearch
        {
            get { return true; }
        }
        #endregion


        protected override void FillClaimTypes(List<string> claimTypes)
        {
            if (claimTypes == null)
                throw new ArgumentNullException("claimTypes");

            // Add our claim type.
            claimTypes.Add(LDAPClaimType);
        }

        protected override void FillClaimValueTypes(List<string> claimValueTypes)
        {
            if (claimValueTypes == null)
                throw new ArgumentNullException("claimValueTypes");

            // Add our claim value type.
            claimValueTypes.Add(LDAPClaimValueType);
        }


        protected override void FillSearch(Uri context, string[] entityTypes,
            string searchPattern, string hierarchyNodeID, int maxCount,
           Microsoft.SharePoint.WebControls.SPProviderHierarchyTree searchTree)
        {
            if (!EntityTypesContain(entityTypes, SPClaimEntityTypes.FormsRole) &&
                !EntityTypesContain(entityTypes, SPClaimEntityTypes.User))
            {
                return;
            }

            List<ADFSUser> users = ADFSHelper.Search(searchPattern);
            foreach (var user in users)
            {
                PickerEntity entity = GetPickerEntity(user);
                searchTree.AddEntity(entity);
            }
        }

        protected override void FillEntityTypes(List<string> entityTypes)
        {
            if (null == entityTypes)
            {
                throw new ArgumentNullException("entityTypes");
            }
            entityTypes.Add(SPClaimEntityTypes.User);
        }


        protected override void FillResolve(Uri context, string[] entityTypes,
            SPClaim resolveInput, List<Microsoft.SharePoint.WebControls.PickerEntity> resolved)
        {
            FillResolve(context, entityTypes, resolveInput.Value, resolved);
        }

        protected override void FillResolve(Uri context, string[] entityTypes,
            string resolveInput, List<Microsoft.SharePoint.WebControls.PickerEntity> resolved)
        {
            ADFSUser user = ADFSHelper.FindExact(resolveInput);
            if (null != user)
            {
                PickerEntity entity = GetPickerEntity(user);
                resolved.Add(entity);
            }
        }


        private PickerEntity GetPickerEntity(ADFSUser user)
        {
            PickerEntity entity = CreatePickerEntity();
           
            entity.Claim = new SPClaim(LDAPClaimType, user.ppl_email, LDAPClaimValueType,
                SPOriginalIssuers.Format(SPOriginalIssuerType.Forms, SPTrustedIdentityTokenIssuerName));
           
            entity.Description = user.ppl_id;
            entity.DisplayText = user.ppl_email;
            entity.EntityData[PeopleEditorEntityDataKeys.DisplayName] = user.ppl_email;
            entity.EntityData[PeopleEditorEntityDataKeys.Email] = user.ppl_email;
            entity.EntityData[PeopleEditorEntityDataKeys.AccountName] = user.ppl_id;
            entity.EntityType = SPClaimEntityTypes.User;
            entity.IsResolved = true;
            return entity;
        }


        #region Not Implemented
        protected override void FillClaimsForEntity(Uri context, SPClaim entity, List<SPClaim> claims)
        {
            throw new NotImplementedException();
        }

        protected override void FillHierarchy(Uri context, string[] entityTypes, string hierarchyNodeID, int numberOfLevels, Microsoft.SharePoint.WebControls.SPProviderHierarchyTree hierarchy)
        {
            throw new NotImplementedException();
        }

        protected override void FillSchema(Microsoft.SharePoint.WebControls.SPProviderSchema schema)
        {
            throw new NotImplementedException();
        }
        #endregion

    }
}
