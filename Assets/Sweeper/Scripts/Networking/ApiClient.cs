using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient
{
    private readonly string _baseUrl;

    public string AccessToken { get; set; }

    public ApiClient(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    public IEnumerator Get<TResponse>(string path, Action<ApiResult<TResponse>> onCompleted)
    {
        string url = _baseUrl + "/" + path.TrimStart('/');

        using UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        ApiResult<TResponse> result = CreateResult<TResponse>(request);

        onCompleted?.Invoke(result);
    }

    public IEnumerator Post<TRequest, TResponse>(string path, TRequest body, Action<ApiResult<TResponse>> onCompleted)
    {
        string url = _baseUrl + "/" + path.TrimStart('/');

        string json = JsonUtility.ToJson(body);

        byte[] rawData = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);

        request.uploadHandler = new UploadHandlerRaw(rawData);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
            );

        yield return request.SendWebRequest();

        ApiResult<TResponse> result = CreateResult<TResponse>(request);

        onCompleted?.Invoke(result);
    }

    private ApiResult<TResponse> CreateResult<TResponse>(
        UnityWebRequest request)
    {
        ApiResult<TResponse> result = new ApiResult<TResponse>();

        result.StatusCode = request.responseCode;

        if(request.result == UnityWebRequest.Result.ConnectionError || 
            request.result == UnityWebRequest.Result.DataProcessingError)
        {
            result.IsSuccess = false;
            result.Error = request.error;

            return result;
        }

        string responseJson = request.downloadHandler.text;

        if (!string.IsNullOrEmpty(responseJson))
        {
            try
            {
                result.Response = JsonUtility.FromJson<TResponse>(responseJson);
            }
            catch (Exception e)
            {
                result.IsSuccess = false;
                result.Error = e.Message;

                return result;
            }
        }

        result.IsSuccess =
            request.responseCode >= 200 &&
            request.responseCode < 300;

        if(!result.IsSuccess)
        {
            result.Error = request.error;
        }

        return result;
    }
}
