
using SkydrmDesktop;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.model
{
    // nxl file convert complete notification callback
    public delegate void NxlConvertCompleteDelegate(NxlConverterResult result);

    [Serializable]
    public class NxlConverterResult
    {
        public string UserEmail { get; }
        public string TmpPath { get; }
        public bool IsConverterSucceed { get; }
        public string ErrorMsg { get; }
        public Int32 ErrorCode { get; }
        public string FileName { get; }
        public long Size { get; }
        public string DateModified { get; }
        public string LocalDiskPath { get; }
        public string RmsRemotePath { get; }
        public bool IsCreatedLocal { get; }
        public string[] SharedWith { get; }
        public FileRights[] Rights { get; }
        public string AdhocWaterMark { get; }
        public Expiration Expiration { get; }
        public string Tags { get; }
        public EnumFileRepo EnumFileRepo { get; }
        public Int32 ProjectId { get; }
        public bool IsDecryptFromRPM { get; }
        public bool IsDisplayEditButton { get; }
        public bool IsDisplayPrintButton { get; }
        public bool IsDisplayShareButton { get; }
        public bool IsDisplaySaveAsButton { get; }
        public bool IsDisplayExtractButton { get; }
        public string ForPrintFilePath { get;}

        // new sdk added
        public bool IsOwner { get; }
        public bool IsByAdHoc { get; }
        public bool IsByCentrolPolicy { get;}

        public bool IsSwitchServer { get; }
        
        private NxlConverterResult(string UserEmail,
                                    string TmpPath,
                                    bool IsConverterSucceed,
                                    string ErrorMsg,
                                    Int32 ErrorCode,
                                    string FileName,
                                    long Size,
                                    string DateModified,
                                    string LocalDiskPath,
                                    string RmsRemotePath,
                                    bool IsCreatedLocal,
                                    string[] SharedWith,
                                    FileRights[] Rights,
                                    string AdhocWaterMark,
                                    Expiration Expiration,
                                    string Tags,
                                    EnumFileRepo EnumFileRepo,
                                    bool IsDecryptFromRPM,
                                    bool IsDisplayEditButton,
                                    bool IsDisplayPrintButton,
                                    bool IsDisplayShareButton,
                                    bool IsDisplaySaveAsButton,
                                    Int32 ProjectId,
                                    bool IsOwner,
                                    bool IsByAdHoc,
                                    bool IsByCentrolPolicy,
                                    bool IsDisplayExtractButton,
                                    string ForPrintFilePath,
                                    bool IsSwitchServer
                                    )
        {
            this.UserEmail = UserEmail;
            this.TmpPath = TmpPath;
            this.IsConverterSucceed = IsConverterSucceed;
            this.ErrorMsg = ErrorMsg;
            this.ErrorCode = ErrorCode;
            this.FileName = FileName;
            this.Size = Size;
            this.DateModified = DateModified;
            this.LocalDiskPath = LocalDiskPath;
            this.RmsRemotePath = RmsRemotePath;
            this.IsCreatedLocal = IsCreatedLocal;
            this.SharedWith = SharedWith;
            this.Rights = Rights;
            this.AdhocWaterMark = AdhocWaterMark;
            this.Expiration = Expiration;
            this.Tags = Tags;
            this.EnumFileRepo = EnumFileRepo;
            this.IsDecryptFromRPM = IsDecryptFromRPM;
            this.IsDisplayEditButton = IsDisplayEditButton;
            this.IsDisplayPrintButton = IsDisplayPrintButton;
            this.IsDisplayShareButton = IsDisplayShareButton;
            this.IsDisplaySaveAsButton = IsDisplaySaveAsButton;
            this.IsDisplayExtractButton = IsDisplayExtractButton;
            this.ProjectId = ProjectId;
            this.ForPrintFilePath = ForPrintFilePath;

            // new sdk added
            this.IsOwner = IsOwner;
            this.IsByAdHoc = IsByAdHoc;
            this.IsByCentrolPolicy = IsByCentrolPolicy;

            this.IsSwitchServer = IsSwitchServer;
        }

        public class Builder
        {
            private string UserEmail { get; set; }
            private string TmpPath { get; set; }
            private bool M_IsConverterSucceed { get; set; }
            private string ErrorMsg { get; set; }
            private Int32 ErrorCode { get; set; }
            private string FileName { get; set; }
            private long Size { get; set; }
            private string DateModified { get; set; }
            private string LocalDiskPath { get; set; }
            private string RmsRemotePath { get; set; }
            private bool M_IsCreatedLocal { get; set; }
            private string[] SharedWith { get; set; }
            private FileRights[] Rights { get; set; }
            private string AdhocWaterMark { get; set; }
            private Expiration Expiration { get; set; }
            private string Tags { get; set; }
            private EnumFileRepo EnumFileRepo { get; set; }
            private Int32 ProjectId { get; set; }
            private bool M_IsDecryptFromRPM { get; set; }
            public bool M_IsDisplayEditButton { get; set; }
            public bool M_IsDisplayPrintButton { get; set; }
            public bool M_IsDisplayShareButton { get; set; }
            public bool M_IsDisplaySaveAsButton { get; set; }
            public bool M_IsDisplayExtractButton { get; set; }
            public string ForPrintFilePath { get; set; }

            // new sdk added
            public bool IsOwner { get; set; }
            public bool IsByAdHoc { get; set; }
            public bool IsByCentrolPolicy { get; set; }

            public bool IsSwitchServer { get; set; }  // is belongs to the server

            public Builder SetUserEmail(string UserEmail)
            {
                this.UserEmail = UserEmail;
                return this;
            }

            public Builder SetTmpPath(string TmpPath)
            {
                this.TmpPath = TmpPath;
                return this;
            }

            public Builder IsConverterSucceed(bool IsConverterSucceed)
            {
                this.M_IsConverterSucceed = IsConverterSucceed;
                return this;
            }

            public Builder SetErrorMsg(string ErrorMsg)
            {
                this.ErrorMsg = ErrorMsg;
                return this;
            }

            public Builder SetErrorCode(Int32 ErrorCode)
            {
                this.ErrorCode = ErrorCode;
                return this;
            }

            public Builder SetFileName(string FileName)
            {

                this.FileName = FileName;

                return this;
            }

            public Builder SetSize(long Size)
            {
                this.Size = Size;
                return this;
            }

            public Builder SetDateModified(string DateModified)
            {

                this.DateModified = DateModified;
                return this;
            }

            public Builder SetLocalDiskPath(string LocalDiskPath)
            {

                this.LocalDiskPath = LocalDiskPath;

                return this;
            }

            public Builder SetRmsRemotePath(string RmsRemotePath)
            {
                this.RmsRemotePath = RmsRemotePath;
                return this;
            }

            public Builder IsCreatedLocal(bool IsCreatedLocal)
            {
                this.M_IsCreatedLocal = IsCreatedLocal;
                return this;
            }

            public Builder SetSharedWith(string[] SharedWith)
            {
                this.SharedWith = SharedWith;
                return this;
            }

            public Builder SetRights(FileRights[] Rights)
            {
                this.Rights = Rights;
                return this;
            }

            public Builder SetAdhocWaterMark(string AdhocWaterMark)
            {

                this.AdhocWaterMark = AdhocWaterMark;

                return this;
            }

            public Builder SetExpiration(Expiration Expiration)
            {
                this.Expiration = Expiration;
                return this;
            }

            public Builder SetTags(string Tags)
            {
                this.Tags = Tags;

                return this;
            }

            public Builder SetEnumFileRepo(EnumFileRepo EnumFileRepo)
            {
                this.EnumFileRepo = EnumFileRepo;
                return this;
            }

            public Builder IsDecryptFromRPM(bool IsDecryptFromRPM)
            {
                this.M_IsDecryptFromRPM = IsDecryptFromRPM;
                return this;
            }

            public Builder IsDisplayEditButton(bool IsDisplayEditButton)
            {
                this.M_IsDisplayEditButton = IsDisplayEditButton;
                return this;
            }

            public Builder IsDisplayPrintButton(bool IsDisplayPrintButton)
            {
                this.M_IsDisplayPrintButton = IsDisplayPrintButton;
                return this;
            }

            public Builder IsDisplayShareButton(bool IsDisplayShareButton)
            {
                this.M_IsDisplayShareButton = IsDisplayShareButton;
                return this;
            }

            public Builder IsDisplaySaveAsButton(bool IsDisplaySaveAsButton)
            {
                this.M_IsDisplaySaveAsButton = IsDisplaySaveAsButton;
                return this;
            }

            public Builder IsDisplayExtractButton(bool IsDisplayExtractButton)
            {
                this.M_IsDisplayExtractButton = IsDisplayExtractButton;
                return this;
            }

            public Builder SetProjectId(Int32 projectId)
            {
                this.ProjectId = projectId;
                return this;
            }

            public Builder SetOwner(bool isOwner)
            {
                this.IsOwner = isOwner;
                return this;
            }

            public Builder SetIsAdHoc(bool isAdhoc)
            {
                this.IsByAdHoc = isAdhoc;
                return this;
            }

            public Builder SetIsCentrolPolicy(bool isByCentrolPolicy)
            {
                this.IsByCentrolPolicy = isByCentrolPolicy;
                return this;
            }

            public Builder SetForPrintFilePath(string ForPrintFilePath)
            {
                this.ForPrintFilePath = ForPrintFilePath;
                return this;
            }

            public Builder SetIsSwitchServer(bool isSwitchServer)
            {
                this.IsSwitchServer = isSwitchServer;
                return this;
            }

            public NxlConverterResult Build()
            {
                SkydrmApp.Singleton.Log.Info("Builder.Build()");
                return new NxlConverterResult(
                    UserEmail, TmpPath, M_IsConverterSucceed,
                    ErrorMsg, ErrorCode, FileName,
                    Size, DateModified, LocalDiskPath, RmsRemotePath,
                    M_IsCreatedLocal, SharedWith, Rights,
                    AdhocWaterMark, Expiration,
                    Tags, EnumFileRepo, M_IsDecryptFromRPM,
                    M_IsDisplayEditButton, M_IsDisplayPrintButton,
                    M_IsDisplayShareButton, M_IsDisplaySaveAsButton,
                    ProjectId, IsOwner,IsByAdHoc,IsByCentrolPolicy,
                    M_IsDisplayExtractButton,
                    ForPrintFilePath, IsSwitchServer);
            }
        }
    }
}
