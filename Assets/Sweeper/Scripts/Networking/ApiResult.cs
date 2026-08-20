using UnityEngine;

public class ApiResult<T>
{
    public bool IsSuccess;
    public long StatusCode;
    public T Response;
    public string Error;
    public string ErrorCode;
}
