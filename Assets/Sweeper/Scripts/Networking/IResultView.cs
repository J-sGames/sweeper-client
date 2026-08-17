using System;
using System.Collections.Generic;

interface IResultView<T>
{
    void OnAwake();
    void OnSuccess(ApiResult<T> result);
    void OnFailed();

}
