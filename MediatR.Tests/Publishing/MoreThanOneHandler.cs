﻿using SharpTestsEx;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MediatR.Tests.Publishing
{
    public class MoreThanOneHandler
    {
        class ServiceLocator
        {
            private readonly Dictionary<Type, List<object>> Services = new Dictionary<Type, List<object>>();

            public void Register(Type type, params object[] implementations)
                => Services.Add(type, implementations.ToList());

            public List<object> Get(Type type) { return Services[type]; }
        }

        public class TasksList
        {
            public List<string> Tasks { get; }

            public TasksList()
            {
                Tasks = new List<string>();
            }
        }

        public class TaskWasAdded : INotification
        {
            public string TaskName { get; }

            public TaskWasAdded(string taskName)
            {
                TaskName = taskName;
            }
        }

        public class TaskWasAddedHandler : INotificationHandler<TaskWasAdded>
        {
            private readonly TasksList _taskList;
            public TaskWasAddedHandler(TasksList tasksList)
            {
                _taskList = tasksList;
            }

            public void Handle(TaskWasAdded @event)
            {
                _taskList.Tasks.Add(@event.TaskName);
            }
        }

        private IMediator mediator;
        private TasksList _taskList = new TasksList();

        public MoreThanOneHandler()
        {
            var eventHandler = new TaskWasAddedHandler(_taskList);

            var serviceLocator = new ServiceLocator();
            serviceLocator.Register(typeof(INotificationHandler<TaskWasAdded>), eventHandler, eventHandler);
            //Registration needed internally by MediatR
            serviceLocator.Register(typeof(IAsyncNotificationHandler<TaskWasAdded>), new IAsyncNotificationHandler<TaskWasAdded>[] { });
            serviceLocator.Register(typeof(ICancellableAsyncNotificationHandler<TaskWasAdded>), new ICancellableAsyncNotificationHandler<TaskWasAdded>[] {  });

            mediator = new Mediator(
                    type => new { },
                    type => serviceLocator.Get(type));
        }

        [Fact]
        public async void GivenTwoHandlersForOneEvent_WhenPublishMethodIsBeingCalled_ThenTwoHandlersAreBeingCalled()
        {
            //Given
            var @event = new TaskWasAdded("cleaning");

            //When
            await mediator.Publish(@event);

            //Then
            _taskList.Tasks.Count.Should().Be.EqualTo(2);
            _taskList.Tasks.Should().Have.SameValuesAs("cleaning", "cleaning");
        }
    }
}
