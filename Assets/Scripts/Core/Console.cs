using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using System.Reflection;
using System.Collections.Generic;
using TMPro;
using System.Linq;

namespace ExoplanetStudios
{
    public class Console : MonoSingleton<Console>
    {
        private Dictionary<string, Action<List<string>>> Commands = new();
        private Dictionary<string, Action<List<string>, Dictionary<string, Action<List<string>>>>> ExeptionCommands = new();

        [Header("ConsoleInput")]
        [SerializeField] private GlobalInputs GI;
        [SerializeField] private GameObject InputFieldObject;
        [SerializeField] private bool AllowConsole;
        [Header("ConsoleOutput")]
        [SerializeField] private ImageFadeController ConsolePanelFadeController;
        [SerializeField] private TMP_Text ConsoleText;
        private ScrollRect consoleScrollRect;

        private TMP_InputField InputField;
        private InputMaster controls;

        private bool active;
        private int index;

        private const string HELP = "help";

        private List<string> commandGroups = new();

        private List<string> History = new();

        /// <summary> 
        /*
        To Implement a new Command, Make a new Function in the Script that should be called with a Command and add the [Command] Attribute.
        Optionally if the Command Name should not be the name of the function, you can add a command Name into the Constructor : [Command("<Command Name>")]
        For the specific help function of a command a description can be added when declaring the Attribute [Command(description="<Description here>")]
        By default the commands are executed on all connected devices. to prevent that add [Command(local=true)]
        Also the Script must derive from MonoBehaviour and IConsoleCommands.

        If this isn't possible (for example in a static class) or the Class isn't instantiated at the beginning of the Game (for example in Prefabs)
        you will need to add the command manually by calling

        Console.AddCommand(<string CommandName>, <FunctionName>);

        To a Function that is called at the beginning of the Game (or when the class is instantiated)
        The script should use System.Collections.Generic

        The Syntax for the new Command-Function should be:

        void Function(List<string> Parameters)
        {
            // Do Something
            
            // you can add Parameters by Adding
            if (Parameters[0] == "parameter"):
                // Do Something else

            // it is also possible to use the Parameter as for example a float:
            try
            {
                AnotherFunctionThatTakesAFloat(float.Parse(Parameters[0]));
            }
            catch (System.Exeption)
            {
                Debug.LogError("Unknown Parameter");
            }
        }

        */
        ///</summary>

        protected override void SingletonAwake()
        {
            // First get all grouped commands
            Type[] methodGroups = Assembly.GetExecutingAssembly().GetTypes()
                                    .Where(t => t.GetCustomAttributes(typeof(CommandGroupAttribute), false).Length > 0).ToArray();

            foreach (Type methodGroup in methodGroups)
            {
                string attributeGroupName = methodGroup.GetCustomAttribute<CommandGroupAttribute>().GroupName;
                string groupName = string.IsNullOrEmpty(attributeGroupName) ? methodGroup.Name : attributeGroupName;
                commandGroups.Add(groupName);
                MethodInfo[] groupMethods = methodGroup.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance) // Include instance methods in the search so we can show the developer an error instead of silently not adding instance methods to the dictionary
                                    .Where(m => m.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0)
                                    .ToArray();
                foreach (MethodInfo method in groupMethods)
                {
                    string attributeCommandName = method.GetCustomAttribute<CommandAttribute>().CommandName;
                    string commandName = string.IsNullOrEmpty(attributeCommandName) ? method.Name : attributeCommandName;
                    AddFoundCommand(method, $"{groupName}.{commandName}");
                }
            }
            // then find all single commands
            MethodInfo[] methods = Assembly.GetExecutingAssembly().GetTypes()
                                    .Where(t => t.GetCustomAttributes(typeof(CommandGroupAttribute), false).Length <= 0)
                                    .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)) // Include instance methods in the search so we can show the developer an error instead of silently not adding instance methods to the dictionary
                                    .Where(m => m.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0)
                                    .ToArray();
            
            foreach (MethodInfo method in methods)
            {
                string attributeCommandName = method.GetCustomAttribute<CommandAttribute>().CommandName;
                AddFoundCommand(method, string.IsNullOrEmpty(attributeCommandName) ? method.Name : attributeCommandName);
            }
            void AddFoundCommand(MethodInfo method, string name)
            {
                if (!method.IsStatic)
                    throw new Exception($"Console methods should be static, but '{method.DeclaringType}.{method.Name}' is an instance method!");
                
                CommandAttribute attribute = method.GetCustomAttribute<CommandAttribute>();

                ParameterInfo[] parameters = method.GetParameters();
                Action<List<string>> action;
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(List<string>))
                {
                    action = (Action<List<string>>)Delegate.CreateDelegate(typeof(Action<List<string>>), method);
                    Commands.Add(name, action);
                }
                else
                    throw new Exception($"Console methods should have a parameter of type List<string>, but '{method.DeclaringType}.{method.Name}' doesn't!");
            }
        }
        public static void AddCommand(string name, Action<List<string>> action)
        {
            Instance.Commands.Add(name, action);
        }
        public static void AddExeption(string name, Action<List<string>, Dictionary<string, Action<List<string>>>> action)
        {
            Instance.ExeptionCommands.Add(name, action);
        }
        void Start()
        {
            InputField = InputFieldObject.GetComponent<TMP_InputField>();
            consoleScrollRect = ConsolePanelFadeController.GetComponent<ScrollRect>();
            controls = GI.Controls;
            controls.Debug.Open.performed += Activate;
            controls.Debug.LoadCommand.performed += LoadHistory;
            controls.Debug.AutoComplete.performed += Autocomplete;
            AddExeption(HELP, GetCommandHelp);
        }
        void OnDestroy()
        {
            controls.Debug.Open.performed -= Activate;
            controls.Debug.LoadCommand.performed -= LoadHistory;
            controls.Debug.AutoComplete.performed -= Autocomplete;
        }
        public static void Print(string message)
        {
            Instance.ConsolePanelFadeController.SetTimer(5f, 2f);
            Instance.ConsoleText.text += "\n" + string.Concat("[>>>] ", message);
            Instance.ConsoleText.ForceMeshUpdate();
            Instance.consoleScrollRect.normalizedPosition = Vector2.zero;
        }
        void Activate(InputAction.CallbackContext ctx)
        {
            if (!AllowConsole || InputStateMachine.AlreadyLocked(this))
                return;
            
            if (!InputFieldObject.activeSelf)
            {
                ConsolePanelFadeController.SetVisible();
                InputStateMachine.ChangeInputState(false, this);
                CursorStateMachine.ChangeCursorState(false, this);
                InputFieldObject.SetActive(true);
                InputField.ActivateInputField();
                active = true;
                index = History.Count;
            }
            else
            {
                ConsolePanelFadeController.UnlockTimer();
                InputStateMachine.ChangeInputState(true, this);
                CursorStateMachine.ChangeCursorState(true, this);
                History.Add(InputField.text);
                List<string> Command = InputField.text.Split(' ').ToList();
                InputField.text = "";
                InputField.DeactivateInputField();
                InputFieldObject.SetActive(false);
                active = false;

                Execute(Command);
            }
        }
        public void Execute(List<string> Command)
        {
            // Execution logic
            if (string.IsNullOrEmpty(Command[0]))
                return;

            // Execute Command Exeptions
            if (ExeptionCommands.ContainsKey(Command[0]))
            {
                ExeptionCommands[Command[0]](Command, Commands);
                return;
            }

            // Execute Command locally
            List<string> parameters = new(Command);
            parameters.RemoveAt(0);

            if (Commands.ContainsKey(Command[0]))
            {
                // if (((CommandAttribute)Commands[Command[0]].GetMethodInfo().GetCustomAttribute(typeof(CommandAttribute), true)).serverOnly && NetworkManager.networkMode == NetworkMode.Client)
                //     return;
            
                // Execute Command
                Commands[Command[0]](parameters);
                Print($"Command {Command[0]} Executed");
            }
            else
                Print("Unknown Command '" + Command[0] + "' Type 'help' for a list of Commands");
        }

        void LoadHistory(InputAction.CallbackContext ctx)
        {
            if (!active)
                return;
            
            try {
                index -= 1;
                InputField.text = History[index];
                InputField.MoveToEndOfLine(false, false);
            }
            catch (Exception) {}
        }
        void Autocomplete(InputAction.CallbackContext ctx)
        {
            if (!active)
                return;
            // first check for command groups
            foreach (string group in commandGroups)
            {
                if (group.StartsWith(InputField.text))
                {
                    InputField.text = group;
                    InputField.MoveToEndOfLine(false, false);
                    return;
                }
            }
            // then check for commands
            foreach (string command in Commands.Keys)
            {
                if (command.StartsWith(InputField.text))
                {
                    InputField.text = command;
                    InputField.MoveToEndOfLine(false, false);
                    return;
                }
            }
        }
        void GetCommandHelp(List<string> Command, Dictionary<string, Action<List<string>>> commandList)
        {
            if (Command.Count < 2)
            {
                LogCommands();
                return;
            }
            Print(((CommandAttribute)commandList[Command[1]].GetMethodInfo().GetCustomAttribute(typeof(CommandAttribute), true)).description);
        }
        public void LogCommands()
        {
            string ListOfCommands = "";
            for (int i = 0; i < Commands.Count; i++)
                ListOfCommands += Commands.ElementAt(i).Key + "\n";
            Debug.Log($"This is a List of all available commands: \n\n{ListOfCommands} \n\nfor more information about a specific command type \"help <CommandName>\" ");
            Print($"This is a List of all available commands: \n\n{ListOfCommands} \n\nfor more information about a specific command type \"help <CommandName>\" ");
        }
        [Command("Say")]
        public static void PrintMessage(List<string> parameters)
        {
            for (int i = 0; i < parameters.Count; i++)
                parameters[i] += " ";
            string Text = string.Concat(parameters);
            Debug.Log(Text);
            Print(Text);
        }
    }
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class CommandAttribute : Attribute
    {
        private string commandName;
        public string description = "No Description";
        public bool serverOnly = false;

        public string CommandName => commandName;

        public CommandAttribute() {}

        public CommandAttribute(string commandName)
        {
            this.commandName = commandName;
        }
    }
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class CommandGroupAttribute : Attribute
    {
        private string groupName;
        public string GroupName => groupName;

        public CommandGroupAttribute() {}

        public CommandGroupAttribute(string groupName)
        {
            this.groupName = groupName;
        }
    }
}