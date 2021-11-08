using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoFinder.Models
{
    public enum Status
    {
        SUCCESS,
        FAIL
    }

    public class Error
    {
        public Error()
        {
            this.Code = 0;
            this.Message = string.Empty;
            this.Description = string.Empty;
        }
        public int Code { get; set; }
        public string Message { get; set; }
        public string Description { get; set; }
    }

    public class Response<T>
    {
        public Response(Status status, T data)
        {
            this.Status = status;
            this.Data = data;
        }
        public Status Status { get; set; }
        public T Data { get; set; }
        
        public Error Error { get; set; }
    }
}
