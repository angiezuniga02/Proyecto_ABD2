using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class DatabaseRecoverySim
{
    static Dictionary<int, string> database = new Dictionary<int, string>();
    static List<string> log = new List<string>();
    static Dictionary<string, (string Password, Role UserRole)> users = new Dictionary<string, (string, Role)>();
    static string checkpointFile = "checkpoint.txt";
    static string logFile = "log.txt"; // Guarda las fechas y horas de alguna operación realizada
    static string authLogFile = "authLog.txt"; // Para verificar las autenticaciones
    static string userFile = "users.txt"; // Para almacenar los usuarios
    static string currentUser = string.Empty; // Para almacenar el usuario actualmente autenticado

    enum Role
    {
        Admin,
        User
    }

    static void Main()
    {
        LoadCheckpoint();
        LoadLog();
        LoadUsers();

        while (true)
        {
            if (AuthenticateUser())
            {
                Console.WriteLine("Autenticación exitosa.");
                break;
            }
            else
            {
                Console.WriteLine("No se pudo autenticarse. Intente nuevamente.");
            }
        }

        while (true)
        {
            Console.WriteLine("\nInicio");
            menu();
        }
    }

    static bool AuthenticateUser()
    {
        Console.WriteLine("\n--- Sistema de Autenticación ---");
        Console.Write("1. Iniciar sesión\n2. Registrarse\nSeleccione una opción: ");
        var option = Console.ReadLine();

        switch (option)
        {
            case "1":
                return LoginUser();
            case "2":
                RegisterUser();
                return AuthenticateUser();
            default:
                Console.WriteLine("Opción no válida.");
                return AuthenticateUser();
        }
    }

    static void RegisterUser()
    {
        Console.Write("Ingrese nombre de usuario: ");
        string username = Console.ReadLine();
        Console.Write("Ingrese contraseña: ");
        string password = Console.ReadLine();

        Console.WriteLine("Seleccione un rol: 1. Admin 2. User");
        string roleInput = Console.ReadLine();
        Role role = roleInput == "1" ? Role.Admin : Role.User;

        if (!users.ContainsKey(username))
        {
            users[username] = (password, role);
            Console.WriteLine("Usuario registrado con éxito.");
            SaveUsers();
        }
        else
        {
            Console.WriteLine("El nombre de usuario ya existe.");
        }
    }

    /*static bool LoginUser()
    {
        Console.Write("Ingrese nombre de usuario: ");
        string username = Console.ReadLine();
        Console.Write("Ingrese contraseña: ");
        string password = Console.ReadLine();

        if (!users.ContainsKey(username))
        {
            Console.WriteLine("El nombre de usuario no está registrado.");
            LogFailedAttempt(username, "usuario");
            return false;
        }

        var userInfo = users[username];
        if (userInfo.Password != password)
        {
            Console.WriteLine("Contraseña incorrecta.");
            LogFailedAttempt(username, "contraseña");
            return false;
        }

        currentUser = username;
        Console.WriteLine($"Rol del usuario: {userInfo.UserRole}");
        return true;
    }*/

    static bool LoginUser()
    {
        Console.Write("Ingrese nombre de usuario: ");
        string username = Console.ReadLine();
        Console.Write("Ingrese contraseña: ");
        string password = Console.ReadLine();

        if (!users.ContainsKey(username))
        {
            Console.WriteLine("El nombre de usuario no está registrado.");
            LogFailedAttempt(username, "usuario");
            return false;
        }

        var userInfo = users[username];
        if (userInfo.Password != password)
        {
            Console.WriteLine("Contraseña incorrecta.");
            LogFailedAttempt(username, "contraseña");
            return false;
        }

        currentUser = username;
        Console.WriteLine($"\nBienvenido {currentUser}!"); 
        Console.WriteLine($"\nRol del usuario: {userInfo.UserRole}");
        return true;
    }

    static void LogFailedAttempt(string username, string errorType)
    {
        using (StreamWriter sw = new StreamWriter(authLogFile, true))
        {
            if (errorType == "usuario")
            {
                sw.WriteLine($"{DateTime.Now} - Fallo de inicio de sesión para: {username} - Error: {errorType} - Usuario: no registrado");
            }
            else if (errorType == "contraseña")
            {
                sw.WriteLine($"{DateTime.Now} - Fallo de inicio de sesión para: {username} - Error: {errorType} - Contraseña: incorrecta");
            }
        }
    }

    static void SaveUsers()
    {
        File.WriteAllLines(userFile, users.Select(u => $"{u.Key} {u.Value.Password} {u.Value.UserRole}"));
    }

    static void LoadUsers()
    {
        if (File.Exists(userFile))
        {
            foreach (var line in File.ReadAllLines(userFile))
            {
                var parts = line.Split(' ');
                if (parts.Length == 3)
                {
                    var role = (Role)Enum.Parse(typeof(Role), parts[2]);
                    users[parts[0]] = (parts[1], role);
                }
            }
        }
    }

    static void menu()
    {
        Console.WriteLine("\nMenu de Operaciones: " +
                           "\n| INSERT id valor " +
                           "\n| UPDATE id valor" +
                           "\n| DELETE id " +
                           "\n| CHECKPOINT " +
                           "\n| CRASH " +
                           "\n| UNDO " +
                           "\n| REDO " +
                           "\n| EXIT");
        Console.Write("Ingrese operación: ");
        string input = Console.ReadLine();
        ProcessCommand(input);
    }

    static void ProcessCommand(string input)
    {
        string[] parts = input.Split(' ');
        if (parts.Length < 1) return;

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string logEntry = $"{timestamp} - {currentUser} - ";

        var userRole = users[currentUser].UserRole;

        switch (parts[0].ToUpper())
        {
            case "INSERT":
                if (userRole == Role.Admin) 
                {
                    if (parts.Length == 3 && int.TryParse(parts[1], out int insertId))
                    {
                        database[insertId] = parts[2];
                        logEntry += $"INSERT {insertId} {parts[2]}";
                        log.Add(logEntry);
                        Console.WriteLine($"Se agregó: Cod {insertId} Nombre: {parts[2]}");
                        SaveLog();
                    }
                }
                else
                {
                    Console.WriteLine("Acceso denegado. Solo los administradores pueden insertar.");
                }
                break;
            case "UPDATE":
                if (userRole == Role.Admin) 
                {
                    if (parts.Length == 3 && int.TryParse(parts[1], out int updateId) && database.ContainsKey(updateId))
                    {
                        string oldValue = database[updateId];
                        database[updateId] = parts[2];
                        logEntry += $"UPDATE {updateId} {oldValue} {parts[2]}";
                        log.Add(logEntry);
                        Console.WriteLine($"Actualizado: {updateId} -> {parts[2]}");
                        SaveLog();
                    }
                }
                else
                {
                    Console.WriteLine("Acceso denegado. Solo los administradores pueden actualizar.");
                }
                break;

            case "DELETE":
                if (userRole == Role.Admin) 
                {
                    if (parts.Length == 2 && int.TryParse(parts[1], out int deleteId) && database.ContainsKey(deleteId))
                    {
                        string deletedValue = database[deleteId];
                        database.Remove(deleteId);
                        logEntry += $"DELETE {deleteId} {deletedValue}";
                        log.Add(logEntry);
                        Console.WriteLine($"Eliminado: {deleteId}");
                        SaveLog();
                    }
                }
                else
                {
                    Console.WriteLine("Acceso denegado. Solo los administradores pueden eliminar.");
                }
                break;
            case "CHECKPOINT":
                CreateCheckpoint();
                break;
            case "CRASH":
                SimulateCrash();
                break;
            case "UNDO":
                UndoLastTransaction();
                break;
            case "REDO":
                RedoTransactions();
                break;
            case "EXIT":
                SaveLog();
                Environment.Exit(0);
                break;
        }
    }
    static void CreateCheckpoint()
    {
        File.WriteAllLines(checkpointFile, database.Select(kv => $"{kv.Key} {kv.Value}"));
        Console.WriteLine("Checkpoint creado.");
    }

    static void LoadCheckpoint()
    {
        if (File.Exists(checkpointFile))
        {
            foreach (var line in File.ReadAllLines(checkpointFile))
            {
                string[] parts = line.Split(' ');
                if (parts.Length == 2 && int.TryParse(parts[0], out int id))
                {
                    database[id] = parts[1];
                }
            }
            Console.WriteLine("Base de datos recuperada desde el checkpoint.");
        }
    }

    static void LoadLog()
    {
        if (File.Exists(logFile))
        {
            log = File.ReadAllLines(logFile).ToList();
        }
    }

    static void SaveLog()
    {
        File.WriteAllLines(logFile, log);
    }

    static void SimulateCrash()
    {
        Console.WriteLine("¡Fallo simulado! Recuperando...");
        LoadCheckpoint();
        Console.WriteLine("Base de datos restaurada al último checkpoint.");
        RedoTransactions();
    }

    static void UndoLastTransaction()
    {
        if (log.Count > 0)
        {
            string lastAction = log.Last();
            log.RemoveAt(log.Count - 1);
            string[] parts = lastAction.Split(' ');

            switch (parts[3])
            {
                case "INSERT":
                    database.Remove(int.Parse(parts[2]));
                    Console.WriteLine($"UNDO: Se eliminó la inserción de {parts[2]}");
                    break;
                case "UPDATE":
                    database[int.Parse(parts[2])] = parts[4];
                    Console.WriteLine($"UNDO: Se revirtió la actualización de {parts[2]} a {parts[4]}");
                    break;
                case "DELETE":
                    database[int.Parse(parts[2])] = parts[4];
                    Console.WriteLine($"UNDO: Se restauró el registro {parts[2]} con valor {parts[4]}");
                    break;
            }
            SaveLog();
        }
        else
        {
            Console.WriteLine("No hay transacciones para deshacer.");
        }
    }

    static void RedoTransactions()
    {
        Console.WriteLine("Reaplicando transacciones confirmadas...");
        foreach (var action in log)
        {
            string[] parts = action.Split(' ');
            switch (parts[3])
            {
                case "INSERT":
                    database[int.Parse(parts[2])] = parts[4];
                    break;
                case "UPDATE":
                    database[int.Parse(parts[2])] = parts[5];
                    break;
                case "DELETE":
                    database.Remove(int.Parse(parts[2]));
                    break;
            }
        }
        Console.WriteLine("REDO completado.");
        SaveLog();
    }
}