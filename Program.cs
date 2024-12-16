using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using twd_to_mobile_sorter.Commands;
using twd_to_pc_sorter.Commands;
using twd_to_pc_sorter.Settings;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var serviceProvider = new ServiceCollection()
    .Configure<MainSettings>(configuration.GetSection(nameof(MainSettings)))
    .AddTransient<SortCommand>()
    .BuildServiceProvider();

var options = serviceProvider.GetService<IOptions<MainSettings>>();

try
{
    switch (options.Value.Mode)
    {
        case Mode.Sort:
            var toPoCommand = serviceProvider.GetService<SortCommand>();
            if (!toPoCommand.HasErrors)
                toPoCommand.Execute();
            break;
        default:
            break;
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
    Console.ReadKey();
}


