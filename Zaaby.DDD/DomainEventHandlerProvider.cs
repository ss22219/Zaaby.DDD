using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Zaaby.DDD.Abstractions.Domain;

namespace Zaaby.DDD
{
    public class DomainEventHandlerProvider
    {
        internal readonly ConcurrentDictionary<Type, List<MethodInfo>>
            SubscriberResolves = new ConcurrentDictionary<Type, List<MethodInfo>>();

        public DomainEventHandlerProvider()
        {
            RegisterDomainEventHandler();
        }

        internal void Register<TDomainEvent, THandler>()
            where TDomainEvent : IDomainEvent
            where THandler : IDomainEventHandler<TDomainEvent>
        {
            var handlerType = typeof(THandler);
            Register<TDomainEvent>(handlerType);
        }

        internal void Register<TDomainEvent>(Type handlerType) where TDomainEvent : IDomainEvent
        {
            var domainEventType = typeof(TDomainEvent);
            Register(domainEventType, handlerType);
        }

        internal void Register(Type domainEventType, Type handlerType)
        {
            if (!SubscriberResolves.ContainsKey(domainEventType))
                SubscriberResolves.TryAdd(domainEventType, new List<MethodInfo>());
            var handleMethod = handlerType.GetMethods().FirstOrDefault(m =>
                m.Name == "Handle" &&
                m.GetParameters().Count() == 1 &&
                m.GetParameters()[0].ParameterType == domainEventType);
            if (handleMethod != null)
                SubscriberResolves[domainEventType].Add(handleMethod);
        }

        private void RegisterDomainEventHandler()
        {
            var domainEventHandlerTypes = ZaabyServerExtension.AllTypes
                .Where(type => type.IsClass && typeof(IDomainEventHandler).IsAssignableFrom(type)).ToList();
            var handlerBaseInterface = typeof(IDomainEventHandler);

            domainEventHandlerTypes.ForEach(domainEventHandlerType =>
            {
                var handlerInterfaces = domainEventHandlerType.GetInterfaces()
                    .Where(i => handlerBaseInterface.IsAssignableFrom(i) && i != handlerBaseInterface);
                foreach (var handlerInterface in handlerInterfaces)
                {
                    var eventType = handlerInterface.GetGenericArguments()[0];
                    Register(eventType, domainEventHandlerType);
                }
            });
        }
    }
}