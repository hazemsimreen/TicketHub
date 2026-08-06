using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLogic.Extensions;

public static class BusinessLogicExtensions
{
    public static IServiceCollection AddBusinessLogic(
        this IServiceCollection services)
    {

        // سيتم تسجيل الخدمات هنا اي خدمة يتم اضافتها في طبقة الاعمال يجب تسجيلها هنا لكي يتم حقنها في الطبقات الاخرى

        return services;
    }
}