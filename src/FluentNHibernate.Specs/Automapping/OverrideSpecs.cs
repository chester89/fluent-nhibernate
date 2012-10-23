using System;
using System.Linq;
using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;
using FluentNHibernate.MappingModel.ClassBased;
using FluentNHibernate.Specs.Automapping.Fixtures;
using FluentNHibernate.Specs.Automapping.Fixtures.Overrides;
using FluentNHibernate.Specs.ExternalFixtures;
using FluentNHibernate.Specs.ExternalFixtures.Overrides;
using Machine.Specifications;

namespace FluentNHibernate.Specs.Automapping
{
    public class when_using_an_automapping_override_to_create_a_join
    {
        Establish context = () =>
            model = AutoMap.Source(new StubTypeSource(typeof(Entity)))
                .Override<Entity>(map =>
                    map.Join("join_table", m => m.Map(x => x.One)));

        Because of = () =>
            mapping = model.BuildMappingFor<Entity>();

        It should_create_the_join_mapping = () =>
            mapping.Joins.ShouldNotBeEmpty();

        It should_have_a_property_in_the_join = () =>
            mapping.Joins.Single().Properties.Select(x => x.Name).ShouldContain("One");

        It should_exclude_the_join_mapped_property_from_the_main_automapping = () =>
            mapping.Properties.Select(x => x.Name).ShouldNotContain("One");
        
        static AutoPersistenceModel model;
        static ClassMapping mapping;
    }

    public class when_using_an_automapping_override_to_specify_a_discriminator
    {
        Establish context = () =>
            model = AutoMap.Source(new StubTypeSource(typeof(Parent), typeof(Child)))
                .Override<Parent>(map =>
                    map.DiscriminateSubClassesOnColumn("discriminator"));

        Because of = () =>
            mapping = model.BuildMappingFor<Parent>();

        It should_map_the_discriminator = () =>
            mapping.Discriminator.ShouldNotBeNull();

        It should_map_subclasses_as_subclass_instead_of_joined_subclass = () =>
        {
            mapping.Subclasses.Count().ShouldEqual(1);
            mapping.Subclasses.ShouldEachConformTo(x => x.SubclassType == SubclassType.Subclass);
        };
        
        static AutoPersistenceModel model;
        static ClassMapping mapping;
    }

    [Subject(typeof(IAutoMappingOverride<>))]
    public class when_using_multiple_overrides_from_different_assemblies
    {
        Establish context = () =>
            model = AutoMap.Source(new StubTypeSource(typeof(Entity)))
                .UseOverridesFromAssemblyOf<EntityBatchSizeOverride>()
                .UseOverridesFromAssemblyOf<EntityTableOverride>();

        Because of = () =>
            mapping = model.BuildMappingFor<Entity>();

        It should_apply_override_from_the_first_assembly = () =>
            mapping.BatchSize.ShouldEqual(1234);

        It should_apply_override_from_the_second_assembly = () =>
            mapping.TableName.ShouldEqual("OverriddenTableName");

        static AutoPersistenceModel model;
        static ClassMapping mapping;
    }

    [Subject(typeof(IAutoMappingOverride<>))]
    public class when_multiple_overrides_present_in_one_class
    {
        Establish context = () =>
        {
            model = AutoMap.Source(new StubTypeSource(typeof(Entity), typeof(Parent), typeof(B_Parent)));
            model.Override(typeof(MultipleOverrides));
        };

        Because of = () =>
        {
            entityMapping = model.BuildMappingFor<Entity>();
            parentMapping = model.BuildMappingFor<Parent>();
            bParentMapping = model.BuildMappingFor<B_Parent>();
        };

        It should_apply_overrides_to_every_class_for_which_such_were_provided = () =>
        {
            entityMapping.EntityName.ShouldEqual("customEntityName");
            parentMapping.TableName.ShouldEqual("fancyTableName_Parent");
            bParentMapping.BatchSize.ShouldEqual(50);
        };
            

        static AutoPersistenceModel model;
        static ClassMapping entityMapping;
        static ClassMapping parentMapping;
        static ClassMapping bParentMapping;
    }

    public class when_overriding_key_column_name_in_base_class
    {
        Establish context = () =>
        {
            model = AutoMap.Source(new StubTypeSource(typeof(Request), typeof(ComercialRequest))); 
            model.Override(typeof(RequestMap));
            model.Override(typeof(CommercialRequestMap));
        };

        Because of = () =>
        {
            baseClassMapping = model.BuildMappingFor<Request>();
            joinedSubclassMapping = model.BuildMappingFor<ComercialRequest>();
        };

        It should_apply_override_to_joined_subclass = () =>
        {
            var idMapping = joinedSubclassMapping.Properties.Single(pm => pm.Member.Name == "Number");
            idMapping.Columns.Single().Name.ShouldEqual("RequestNumber"); 
        };

        static AutoPersistenceModel model;
        static ClassMapping baseClassMapping;
        static ClassMapping joinedSubclassMapping;
    }

    public class MultipleOverrides: IAutoMappingOverride<Entity>, IAutoMappingOverride<Parent>, IAutoMappingOverride<B_Parent>
    {
        public void Override(AutoMapping<Entity> mapping)
        {
            mapping.EntityName("customEntityName");
        }

        public void Override(AutoMapping<Parent> mapping)
        {
            mapping.Table("fancyTableName_Parent");
        }

        public void Override(AutoMapping<B_Parent> mapping)
        {
            mapping.BatchSize(50);
        }
    }

    public class Request
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string ChildType { get; set; }
        public string Type { get; set; }
    }

    public class ComercialRequest: Request
    {
        public DateTime Time { get; set; }
    }

    public class RequestMap : IAutoMappingOverride<Request>
    {
        public void Override(AutoMapping<Request> mapping)
        {
            mapping.Polymorphism.Explicit();

            mapping.Table("Request");
            mapping.Id(c => c.Id, "RequestNumber").GeneratedBy.Assigned();
            mapping.Map(c => c.Date, "ActualDate");
            mapping.Map(c => c.ChildType, "Child");
            mapping.Map(c => c.Type, "RequestType");

            mapping.JoinedSubClass<ComercialRequest>("RequestNumber");
        }
    }

    class CommercialRequestMap: IAutoMappingOverride<ComercialRequest>
    {
        public void Override(AutoMapping<ComercialRequest> mapping)
        {
            mapping.Map(c => c.Time, "ActualTime");
        }
    }
}
