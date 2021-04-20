﻿using Liquid.Core.Telemetry;
using Liquid.Data.EntityFramework.Exceptions;
using Liquid.Data.EntityFramework.Tests.Entities;
using Liquid.Data.EntityFramework.Tests.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Liquid.Data.EntityFramework.Tests.TestCases
{
    [TestFixture()]
    public class EntityFrameworkRepositoryTest
    {
        private IServiceProvider _serviceProvider;

        [SetUp]
        public async Task EstablishContext()
        {
            var services = new ServiceCollection();
            var databaseName = $"TEMP_{Guid.NewGuid()}";

            services.AddDbContext<MockDbContext>(options => options.UseInMemoryDatabase(databaseName: databaseName));
            services.AddScoped<IMockRepository, MockRepository>();
            services.AddTransient<ILightTelemetryFactory>((s) => Substitute.For<ILightTelemetryFactory>());

            _serviceProvider = services.BuildServiceProvider();

            await SeedDataAsync(_serviceProvider);
        }

        [Category("AddAsync")]
        [Test]
        public async Task Verify_insert()
        {
            //Arrange
            IMockRepository mockRepository = GenerateMockRepository();
            var entity = new MockEntity { MockTitle = "TITLE", Active = true, CreatedDate = DateTime.Now };

            //Act
            await mockRepository.AddAsync(entity);

            //Assert
            Assert.NotNull(entity);
            Assert.AreNotEqual(default, entity.MockId);
        }

        [Category("AddAsync")]
        [Test]
        public async Task Verify_insert_Except()
        {
            //Arrange
            var dbSet = Substitute.For<DbSet<MockEntity>, IQueryable<MockEntity>>();
            dbSet.When(o => o.AddAsync(Arg.Any<MockEntity>())).Do((call) => throw new Exception());
            IMockRepository mockRepository = GenerateMockRepository(dbSet);
            var entity = new MockEntity { MockTitle = "TITLE", Active = true, CreatedDate = DateTime.Now };

            //Act
            var task = mockRepository.AddAsync(entity);

            //Assert
            Assert.ThrowsAsync<EntityFrameworkException>(() => task);
        }

        [Category("FindByIdAsync")]
        [Test]
        public async Task Verify_find_by_id()
        {
            //Arrange
            IMockRepository mockRepository = GenerateMockRepository();
            var mockId = 1;

            //Act
            var entity = await mockRepository.FindByIdAsync(mockId);

            //Assert
            Assert.NotNull(entity);
            Assert.AreEqual(mockId, entity.MockId);
        }
        [Category("FindByIdAsync")]
        [Test]
        public async Task Verify_find_by_id_Except()
        {
            //Arrange
            var dbSet = Substitute.For<DbSet<MockEntity>, IQueryable<MockEntity>>();
            IMockRepository mockRepository = GenerateMockRepository(dbSet);
            var mockId = 1;

            //Act
            var task = mockRepository.FindByIdAsync(mockId);

            //Assert
            Assert.ThrowsAsync<EntityFrameworkException>(() => task);
        }

        [Category("WhereAsync")]
        [Test]
        public async Task Verify_where()
        {
            //Arrange
            IMockRepository mockRepository = GenerateMockRepository();
            string mockTitle = "TITLE_002";

            //Act
            var result = await mockRepository.WhereAsync(o => o.MockTitle.Equals(mockTitle));

            //Assert
            Assert.NotNull(result);
            Assert.IsNotEmpty(result);
            Assert.IsTrue(result.All(o => o.MockTitle.Equals(mockTitle)));
        }

        [Category("WhereAsync")]
        [Test]
        public async Task Verify_where_Except()
        {
            //Arrange
            //Arrange
            var dbSet = Substitute.For<DbSet<MockEntity>, IQueryable<MockEntity>>();
            IMockRepository mockRepository = GenerateMockRepository(dbSet);
            string mockTitle = "TITLE_002";

            //Act
            var task = mockRepository.WhereAsync(o => o.MockTitle.Equals(mockTitle));

            //Assert
            Assert.ThrowsAsync<EntityFrameworkException>(() => task);
        }

        [Category("WhereByPageAsync")]
        [Test]
        public async Task Verify_WhereByPageAsync()
        {
            //Arrange
            IMockRepository mockRepository = GenerateMockRepository();
            var lastMockId = 10;
            var pageSize = 10;

            //Act
            var result = await mockRepository.WhereByPageAsync(o => o.Active, 0, pageSize, o => o.MockId);

            //Assert
            Assert.NotNull(result);
            Assert.IsNotEmpty(result);
            Assert.AreEqual(pageSize, result.Count());
            Assert.AreEqual(lastMockId, result.LastOrDefault().MockId);
        }

        [Category("WhereByPageAsync")]
        [Test]
        public async Task Verify_WhereByPageAsync_NoOrderBy()
        {
            //Arrange
            IMockRepository mockRepository = GenerateMockRepository();
            var lastMockId = 10;
            var pageSize = 10;

            //Act
            var result = await mockRepository.WhereByPageAsync(o => o.Active, 0, pageSize);

            //Assert
            Assert.NotNull(result);
            Assert.IsNotEmpty(result);
            Assert.AreEqual(pageSize, result.Count());
            Assert.AreEqual(lastMockId, result.LastOrDefault().MockId);
        }
        
        [Category("WhereByPageAsync")]
        [Test]
        public async Task Verify_WhereByPageAsync_OrderByDesc()
        {
            //Arrange
            IMockRepository mockRepository = GenerateMockRepository();
            var lastMockId = 91;
            var pageSize = 10;

            //Act
            var result = await mockRepository.WhereByPageAsync(o => o.Active, 0, pageSize, o => o.MockId, false);

            //Assert
            Assert.NotNull(result);
            Assert.IsNotEmpty(result);
            Assert.AreEqual(pageSize, result.Count());
            Assert.AreEqual(lastMockId, result.LastOrDefault().MockId);
        }
        
        [Category("WhereByPageAsync")]
        [Test]
        public async Task Verify_WhereByPageAsync_Except()
        {
            //Arrange
            var dbSet = Substitute.For<DbSet<MockEntity>, IQueryable<MockEntity>>();
            IMockRepository mockRepository = GenerateMockRepository(dbSet);

            //Act
            var task = mockRepository.WhereByPageAsync(o => o.Active, 0, 10);

            //Assert
            Assert.ThrowsAsync<EntityFrameworkException>(() => task);
        }

        [Category("FindAllAsync")]
        [Test]
        public async Task Verify_find_all()
        {
            //Arrange
            IMockRepository mockRepository = GenerateMockRepository();

            //Act
            var result = await mockRepository.FindAllAsync();

            //Assert
            Assert.NotNull(result);
            Assert.IsNotEmpty(result);
            Assert.AreEqual(100, result.Count());
        }

        [Category("FindAllAsync")]
        [Test]
        public async Task Verify_find_all_Except()
        {
            //Arrange
            var dbSet = Substitute.For<DbSet<MockEntity>, IQueryable<MockEntity>>();
            var telemetryFactory = Substitute.For<ILightTelemetryFactory>();
            var telemetryClient = Substitute.For<ILightTelemetry>();
            telemetryClient.When(o => o.EnqueueContext(Arg.Any<string>())).Do((call) => throw new Exception());
            telemetryFactory.GetTelemetry().Returns(telemetryClient);
            IMockRepository mockRepository = GenerateMockRepository(dbSet, telemetryFactory);

            //Act
            var task = mockRepository.FindAllAsync();

            //Assert
            Assert.ThrowsAsync<EntityFrameworkException>(() => task);
        }

        [Category("RemoveAsync")]
        [Test]
        public async Task Verify_delete()
        {
            //Arrange
            IMockRepository mockRepository = GenerateMockRepository();
            var mockId = 1;

            //Act
            var entity = await mockRepository.FindByIdAsync(mockId);
            await mockRepository.RemoveAsync(entity);
            var anotherEntity = await mockRepository.FindByIdAsync(mockId);

            //Assert
            Assert.IsNull(anotherEntity);
        }
        [Category("RemoveAsync")]
        [Test]
        public async Task Verify_delete_invalid()
        {
            //Arrange
            IMockRepository mockRepository = GenerateMockRepository();
            var mockId = 101;

            //Act
            var entity = await mockRepository.FindByIdAsync(mockId);
            await mockRepository.RemoveAsync(new MockEntity() { MockId = mockId });
            var anotherEntity = await mockRepository.FindByIdAsync(mockId);

            //Assert
            Assert.IsNull(entity);
            Assert.IsNull(anotherEntity);
        }

        [Category("RemoveAsync")]
        [Test]
        public async Task Verify_delete_Except()
        {
            //Arrange
            var dbSet = Substitute.For<DbSet<MockEntity>, IQueryable<MockEntity>>();
            IMockRepository mockRepository = GenerateMockRepository(dbSet);
            var mockId = 1;

            //Act
            var task = mockRepository.RemoveAsync(new MockEntity() { MockId = mockId });

            //Assert
            Assert.ThrowsAsync<EntityFrameworkException>(() => task);
        }

        [Category("UpdateAsync")]
        [Test]
        public async Task Verify_updates()
        {
            //Arrange
            IMockRepository mockRepository = GenerateMockRepository();
            var mockId = 1;

            //Act
            var entity = await mockRepository.FindByIdAsync(mockId);
            entity.MockTitle = $"TITLE_001_UPDATED";
            await mockRepository.UpdateAsync(entity);
            var anotherEntity = await mockRepository.FindByIdAsync(mockId);

            //Assert
            Assert.NotNull(anotherEntity);
            Assert.AreEqual("TITLE_001_UPDATED", anotherEntity.MockTitle);
        }

        [Category("UpdateAsync")]
        [Test]
        public async Task Verify_updates_Except()
        {
            //Arrange
            var dbSet = Substitute.For<DbSet<MockEntity>, IQueryable<MockEntity>>();
            IMockRepository mockRepository = GenerateMockRepository(dbSet);
            var mockId = 1;

            //Act
            var task = mockRepository.UpdateAsync(new MockEntity() { MockId = mockId });

            //Assert
            Assert.ThrowsAsync<EntityFrameworkException>(() => task);
        }

        private async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            MockDbContext dbContext = serviceProvider.GetService<MockDbContext>();

            for (int i = 1; i <= 100; i++)
            {
                await dbContext.AddAsync(new MockEntity() { MockId = i, MockTitle = $"TITLE_{i:000}", Active = true, CreatedDate = DateTime.Now });
            }
            await dbContext.SaveChangesAsync();
        }

        private IMockRepository GenerateMockRepository()
        {
            return _serviceProvider.GetService<IMockRepository>();
        }

        private IMockRepository GenerateMockRepository(DbSet<MockEntity> dbSet, ILightTelemetryFactory telemetryFactory = null)
        {
            var dbContext = Substitute.For<MockDbContext>();
            dbContext.Set<MockEntity>().Returns(dbSet);

            telemetryFactory = telemetryFactory ?? _serviceProvider.GetService<ILightTelemetryFactory>();
            
            return new MockRepository(dbContext, telemetryFactory);
        }
    }
}