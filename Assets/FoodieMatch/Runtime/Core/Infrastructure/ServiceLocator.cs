using System;
using System.Collections.Generic;
namespace FoodieMatch.Runtime.Core.Infrastructure
{
    public sealed class ServiceLocator
    {
        private readonly Dictionary<Type, object> _services = new();

        public void Register<TService>(TService service)
            where TService : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            Type serviceType = typeof(TService);
            _services[serviceType] = service;
        }

        public TService Get<TService>()
            where TService : class
        {
            Type serviceType = typeof(TService);

            if (!_services.TryGetValue(serviceType, out object service))
            {
                throw new InvalidOperationException($"Service not found: {serviceType.Name}");
            }

            return service as TService;
        }

        public bool TryGet<TService>(out TService service)
            where TService : class
        {
            Type serviceType = typeof(TService);

            if (_services.TryGetValue(serviceType, out object serviceObject))
            {
                service = serviceObject as TService;
                return service != null;
            }

            service = null;
            return false;
        }

        public void Clear()
        {
            _services.Clear();
        }
    }
}


