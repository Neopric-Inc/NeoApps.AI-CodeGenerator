using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using nkv.MicroService.Model;
using System.Collections.Concurrent;
using nkv.MicroService.DataAccess.Interface;
using nkv.MicroService.Manager.Interface;
using nkv.MicroService.Manager.Impl;
using Newtonsoft.Json;
using MassTransit.Configuration;
using MySql.Data.MySqlClient;
using System.Configuration;
using nkv.MicroService.DataAccess.Impl;
using nkv.MicroService.Utility;
using Microsoft.AspNetCore.Hosting.Server;
using System;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using nkv.MicroService.Manager.RabitMQAPI.API;

namespace nkv.MicroService.Consumer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
            string connectionString = configuration.GetConnectionString("MySQLDatabase");
            string rabbitMqConnection = configuration.GetConnectionString("RabitMQ");
            services.AddScoped<IRabitMQProducer, RabitMQProducer>();
            services.AddScoped<IRabitMQAsyncProducer, RabitMQAsyncProducer>();
            Console.WriteLine(connectionString);
            services.AddSingleton(_ => new MySqlDatabase(connectionString));
            //services.AddTransient(_ => new RabbitMqConnection(rabbitMqConnection));
            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri(rabbitMqConnection);
            IConnection connection = factory.CreateConnection();
            services.AddSingleton<IConnection>(connection);
            var serviceProvider = services.BuildServiceProvider();
            ConsumeMessage cm = new ConsumeMessage(connection);
            cm.consumeMessage();
        }
        private static void ConfigureServices(IServiceCollection services)
        {
            #region Dependency

            services.AddTransient<IWorkflowDataAccess, WorkflowDataAccess>();
            services.AddTransient<IWorkflowsDataAccess, WorkflowsDataAccess>();
            services.AddTransient<IWorkflows_projectsDataAccess, Workflows_projectsDataAccess>();
            services.AddTransient<IWorkflow_buildsDataAccess, Workflow_buildsDataAccess>();
            services.AddTransient<IWorkflow_deploymentsDataAccess, Workflow_deploymentsDataAccess>();
            services.AddTransient<IWorkflow_runsDataAccess, Workflow_runsDataAccess>();
            services.AddTransient<IWorkflow_triggersDataAccess, Workflow_triggersDataAccess>();
            services.AddTransient<IWorkflow_trigger_conditionsDataAccess, Workflow_trigger_conditionsDataAccess>();
            services.AddTransient<IWorkflowManager, WorkflowManager>();
            services.AddTransient<IWorkflowsManager, WorkflowsManager>();
            services.AddTransient<IWorkflows_projectsManager, Workflows_projectsManager>();
            services.AddTransient<IWorkflow_buildsManager, Workflow_buildsManager>();
            services.AddTransient<IWorkflow_deploymentsManager, Workflow_deploymentsManager>();
            services.AddTransient<IWorkflow_runsManager, Workflow_runsManager>();
            services.AddTransient<IWorkflow_triggersManager, Workflow_triggersManager>();
            services.AddTransient<IWorkflow_trigger_conditionsManager, Workflow_trigger_conditionsManager>();
            services.AddSingleton<IChannelManager, ChannelManager>();
            #endregion
        }
    }
}
