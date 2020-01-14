﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Cofoundry.Core.DependencyInjection;

namespace Cofoundry.Domain
{
    public class CustomEntitiesDependencyRegistration : IDependencyRegistration
    {
        public void Register(IContainerRegister container)
        {
            var singletonOptions = RegistrationOptions.SingletonScope();

            container
                .Register<ICustomEntityDisplayModelMapper, CustomEntityDisplayModelMapper>()
                .Register<ICustomEntityDataModelMapper, CustomEntityDataModelMapper>()
                .Register<ICustomEntityCache, CustomEntityCache>()
                .Register<ICustomEntityRepository, CustomEntityRepository>()
                .RegisterAll<ICustomEntityRoutingRule>(singletonOptions)
                .RegisterAll<ICustomEntityDefinition>(singletonOptions)
                .RegisterAll<ICustomEntityDisplayModel>()
                .RegisterAllGenericImplementations(typeof(ICustomEntityDisplayModelMapper<,>))
                .RegisterSingleton<ICustomEntityDefinitionRepository, CustomEntityDefinitionRepository>()
                .Register<ICustomEntityRenderSummaryMapper, CustomEntityRenderSummaryMapper>()
                .Register<ICustomEntitySummaryMapper, CustomEntitySummaryMapper>()
                .Register<ICustomEntityVersionSummaryMapper, CustomEntityVersionSummaryMapper>()
                .Register<ICustomEntityDefinitionMicroSummaryMapper, CustomEntityDefinitionMicroSummaryMapper>()
                .Register<ICustomEntityDefinitionSummaryMapper, CustomEntityDefinitionSummaryMapper>()
                .Register<ICustomEntityRouteMapper, CustomEntityRouteMapper>()
                .Register<ICustomEntityRouteDataBuilderFactory, CustomEntityRouteDataBuilderFactory>()
                .Register<ISearchSpecificationMapper<Expression>,ExpressionSearchSpecificationMapper>()
                .RegisterAllGenericImplementations(typeof(ICustomEntityRouteDataBuilder<,>))
                ;
        }
    }
}
