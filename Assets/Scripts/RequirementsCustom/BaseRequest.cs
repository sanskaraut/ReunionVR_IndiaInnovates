using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace ApiHandling
{
    public class BaseResponse
    {
        public bool status;
        public string message;
    }

    public class EncryptedRequest
    {
        public byte[] data;
    }

    public interface IRequest
    {
        string EndPointKey { get; }
    }
    
    public abstract class BaseRequest<TResponse> : IRequest
    where TResponse : BaseResponse
    {
        [JsonIgnore] public abstract string EndPointKey { get; }
        [JsonIgnore] public abstract ApiType ApiType { get; }
        [JsonIgnore] public Dictionary<string, string> headers;

        [JsonIgnore] public DateTime RequestSentTime { get; internal set; }
        [JsonIgnore] public DateTime ResponseReceivedTime { get; internal set; }
        [JsonIgnore] public UnityWebRequest.Result Result { get; internal set; }
        [JsonIgnore] public string Error;

        [JsonIgnore]
        public string this[string key]
        {
            get => headers == null ? "" : headers[key];
            set
            {
                if (headers == null)
                    headers = new Dictionary<string, string>();
                headers[key] = value;
            }
        }
    }
}