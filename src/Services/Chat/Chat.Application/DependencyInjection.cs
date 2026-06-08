using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Chat.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.LicenseKey = "eyJhbGciOiJSUzI1NiIsImtpZCI6Ikx1Y2t5UGVubnlTb2Z0d2FyZS5jb20iLCJhdWQiOiJMdWNreVBlbm55U29mdHdhcmUiLCJleHAiOiIxNzg2NDkyODAwIiwiaWF0IjoiMTc1NTAwMjM2MyIsImFjY291bnRfaWQiOiIwMTk4OWU0NmFjZDM3NjEwOTBlZWM2NmY3YmMyZGIxOSIsImN1c3RvbWVyX2lkIjoiY3RtXzAxazJmNG5xanpyNTc0d2JqdzVwaDFhOWttIiwic3ViX2lkIjoiLSIsImVkaXRpb24iOiIwIiwidHlwZSI6IjIifQ.dP6orFkxFMAkUumtY36WJIg0fUzRi4Lqhvc2RYNlQFCCSpu1lEJnblyhuM_p_fSdbksZr5hLP3G45ul1UgCvCm-AL1pYOUZ_2aOkoGtdosJi724hSObBpa_1JgnrBnDbmW_8ZQJDPW6ycNHT-Ov6Y9Mah2Yi5umVu4viwhOyLYgYU_ddruQOqzgSv5cpqdUJboFgU1gRJ_Uq2zlSZO8ZqvxYfGehQfq93f_jo3_8kdKHYOiJUszdJHjurZe4USljGox3-abq5lZU4pWv3hAiMrZiryOWjL6dbHtM0RDLrbJw4NI2zS84wBBeg7K5ahJ1QD6i9hIh-0TC2IFr-ydxvg";
        });

        return services;
    }
}
