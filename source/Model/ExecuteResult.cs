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
            // GRPC 的状态码，若无，则默认为 -1
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

        public static implicit operator ExecuteResult(Exception ex)
        {
            return new ExecuteResult
            {
                Exception = ex,
            };
        }

        public static ExecuteResult Success()
        {
            return new ExecuteResult
            {
                StatusCode = 0,
                Message = "操作成功"
            };
        }
        public static ExecuteResult Fail(string message, int statusCode = -1)
        {
            return new ExecuteResult
            {
                StatusCode = statusCode,
                Message = message
            };
        }
    }

    internal partial class ExecuteResult<T> : ExecuteResult where T : class
    {
        // 这个类型的 Data 暂时不需要考虑 MVVM 的通知属性

        /// <summary>
        /// 数据内容
        /// </summary>
        public T? Data { get; set; }

        public ExecuteResult()
        {
        }


        public static implicit operator ExecuteResult<T>(T data)
        {
            return new ExecuteResult<T>
            {
                Data = data
            };
        }
    }
}
