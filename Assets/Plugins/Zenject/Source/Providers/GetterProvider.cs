using System;
using System.Collections.Generic;
using ModestTree;

namespace Zenject
{
[NoReflectionBaking]
public class GetterProvider<TObj, TResult> : IProvider
{
    private readonly DiContainer _container;
    private readonly object _identifier;
    private readonly bool _matchAll;
    private readonly Func<TObj, TResult> _method;
    private readonly InjectSources _sourceType;

    public GetterProvider(
        object identifier, Func<TObj, TResult> method,
        DiContainer container, InjectSources sourceType, bool matchAll)
    {
        _container = container;
        _identifier = identifier;
        _method = method;
        _matchAll = matchAll;
        _sourceType = sourceType;
    }

    public bool IsCached => false;

    public bool TypeVariesBasedOnMemberType => false;

    public Type GetInstanceType(InjectContext context)
    {
        return typeof(TResult);
    }

    public void GetAllInstancesWithInjectSplit(
        InjectContext context, List<TypeValuePair> args, out Action injectAction, List<object> buffer)
    {
        Assert.IsEmpty(args);
        Assert.IsNotNull(context);

        Assert.That(typeof(TResult).DerivesFromOrEqual(context.MemberType));

        injectAction = null;

        if (_container.IsValidating)
        {
            // All we can do is validate that the getter object can be resolved
            if (_matchAll)
                _container.ResolveAll(GetSubContext(context));
            else
                _container.Resolve(GetSubContext(context));

            buffer.Add(new ValidationMarker(typeof(TResult)));
            return;
        }

        if (_matchAll)
        {
            Assert.That(buffer.Count == 0);
            _container.ResolveAll(GetSubContext(context), buffer);

            for (var i = 0; i < buffer.Count; i++) buffer[i] = _method((TObj)buffer[i]);
        }
        else
        {
            buffer.Add(_method(
                (TObj)_container.Resolve(GetSubContext(context))));
        }
    }

    private InjectContext GetSubContext(InjectContext parent)
    {
        var subContext = parent.CreateSubContext(
            typeof(TObj), _identifier);

        subContext.Optional = false;
        subContext.SourceType = _sourceType;

        return subContext;
    }
}
}