using SkydrmLocal.rmc.featureProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.fileSystem.project
{
    // UI will bind this date struct 
    public class ProjectInfo
    {
        public Int32 ProjectId { get; set; }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public bool BOwner { get; set; } // Flag that by me or by others.

        public Int64 TotalFiles { get; set; }

        public string TenantId { get; set; }

        public string MemberShipId { get; set; }

        public IMyProject Raw { get; set; }

        public ProjectInfo(IMyProject myProject)
        {
            this.ProjectId = myProject.Id;
            this.Name = myProject.DisplayName;
            this.DisplayName = myProject.DisplayName;
            this.Description = myProject.Description;
            this.BOwner = myProject.IsOwner;
            this.MemberShipId = myProject.MemberShipId;
            this.TenantId = "";

            this.Raw = myProject;
        }

        public static bool operator ==(ProjectInfo lhs, ProjectInfo rhs)
        {
            return Equals(lhs, rhs);
        }

        public static bool operator !=(ProjectInfo lhs, ProjectInfo rhs)
        {
            return !Equals(lhs, rhs);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (this.GetType() != obj.GetType())
            {
                return false;
            }

            return this.ProjectId == (obj as ProjectInfo).ProjectId &&
                    this.Name == (obj as ProjectInfo).Name &&
                    this.DisplayName == (obj as ProjectInfo).DisplayName &&
                    this.Description == (obj as ProjectInfo).Description &&
                    this.BOwner == (obj as ProjectInfo).BOwner &&
                    this.MemberShipId == (obj as ProjectInfo).MemberShipId &&
                    this.TenantId == (obj as ProjectInfo).TenantId;
        }

        public override int GetHashCode()
        {
            return ProjectId.GetHashCode();
        }
    }
}
