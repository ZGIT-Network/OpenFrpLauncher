using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenFrp.Launcher.Model
{
    internal partial class UserInfo : ObservableObject
    {
        public UserInfo()
        {
            UserInfomation = new Yue3.Model.OpenFrp.Response.Data.UserInfo { };
        }

        public UserInfo(Yue3.Model.OpenFrp.Response.Data.UserInfo userInfo)
        {
            this.UserInfomation = userInfo;
        }

        [ObservableProperty]
        private Yue3.Model.OpenFrp.Response.Data.UserInfo? userInfomation;

        public byte[] GetTunnelJsonBuffer()
        {
            if (UserInfomation is null) return Array.Empty<byte>();

            return _buffer ??= JsonSerializer.SerializeToUtf8Bytes(UserInfomation);
        }

        private byte[]? _buffer;

        /// <summary>
        /// 用户邮箱
        /// </summary>
        public string Email { get => UserInfomation?.Email ?? throw new NullReferenceException(nameof(Email));}

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get => UserInfomation?.UserName ?? throw new NullReferenceException(nameof(UserName));}

        /// <summary>
        /// 用户所在组名称
        /// </summary>
        public string GroupCName { get => UserInfomation?.GroupCName ?? throw new NullReferenceException(nameof(GroupCName));}
        /// <summary>
        /// 用户所在组
        /// </summary>
        public string Group { get => UserInfomation?.Group ?? throw new NullReferenceException(nameof(Group)); }

        /// <summary>
        /// 用户 ID
        /// </summary>
        public int UserID { get => UserInfomation?.UserID ?? throw new NullReferenceException(nameof(UserID)); }

        /// <summary>
        /// 最大隧道数量
        /// </summary>
        public int MaxProxies { get => UserInfomation?.MaxProxies ?? throw new NullReferenceException(nameof(MaxProxies)); }
        /// <summary>
        /// 已使用的隧道数
        /// </summary>
        public int UsedProxies { get => UserInfomation?.UsedProxies ?? throw new NullReferenceException(nameof(UsedProxies)); }

        /// <summary>
        /// 可用流量
        /// </summary>
        public long Traffic { get => UserInfomation?.Traffic ?? throw new NullReferenceException(nameof(Traffic)); }

        /// <summary>
        /// 用户 Token
        /// </summary>
        [DebuggerHidden()]
        public string UserToken { get => UserInfomation?.UserToken ?? throw new NullReferenceException(nameof(UserToken)); }

        /// <summary>
        /// 是否已实名
        /// </summary>
        public bool IsRealname { get => UserInfomation?.IsRealname ?? throw new NullReferenceException(nameof(IsRealname)); }
        /// <summary>
        /// 入口带宽速率
        /// </summary>
        public int InputLimit { get => UserInfomation?.InputLimit ?? throw new NullReferenceException(nameof(InputLimit)); }
        /// <summary>
        /// 出口带宽速率
        /// </summary>
        public int OutputLimit { get => UserInfomation?.OutputLimit ?? throw new NullReferenceException(nameof(OutputLimit)); }

        /// <summary>
        /// 注册时间
        /// </summary>
        public string? RegisterTime { get => UserInfomation?.RegisterTime ?? throw new NullReferenceException(nameof(RegisterTime)); }
    }
}
