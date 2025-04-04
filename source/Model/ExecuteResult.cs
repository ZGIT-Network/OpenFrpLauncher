using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenFrp.Launcher.Model
{
    internal partial class ExecuteResult : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasException))]
        private Exception? exception;

        public bool HasException => Exception is not null;

        [ObservableProperty]
        private string? message;

        [ObservableProperty]
        private int statusCode;

        public ExecuteResult()
        {
            
        }

        public ExecuteResult(Yue3.Model.Result.HttpResponse response)
        {
            Exception = response.Exception;
            Message = response.Message;
            StatusCode = (int)response.StatusCode;
        }

        public ExecuteResult(OpenFrp.Service.Proto.RpcResponse response)
        {
            Exception = response.Exception;
            Message = "远程调用 RPC 失败";
            StatusCode = (int?)response.StatusCode ?? -1;
        }

        public static implicit operator ExecuteResult(Yue3.Model.Result.HttpResponse response)
        {
            return new ExecuteResult
            {
                Exception = response.Exception,
                Message = response.Message,
                StatusCode = (int)response.StatusCode
            };
        }
        public static implicit operator ExecuteResult(OpenFrp.Service.Proto.RpcResponse response)
        {
            return new ExecuteResult
            {
                Exception = response.Exception,
                Message = "远程调用 RPC 失败",
                StatusCode = (int?)response.StatusCode ?? -1
            };
        }
    }
}
