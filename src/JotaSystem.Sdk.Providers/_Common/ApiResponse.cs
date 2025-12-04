namespace JotaSystem.Sdk.Providers.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public T? Data { get; set; }

        public static ApiResponse<T> CreateSuccess(T data) =>
            new() { Success = true, Data = data };

        public static ApiResponse<T> CreateFail(string error) =>
            new() { Success = false, ErrorMessage = error };
    }
}