namespace GDBAPI.DtoModels
{
    public class ErrorResponseDto<T>
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
}
