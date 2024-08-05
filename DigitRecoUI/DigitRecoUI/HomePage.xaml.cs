using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Newtonsoft.Json;
using SkiaSharp;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace DigitRecoUI;

public partial class HomePage : ContentPage
{
    // Carpeta creada previamente en la que está guardado el modelo y donde se guardarán las imágenes
    static string dataPath = AppDomain.CurrentDomain.BaseDirectory + @"data\";

    public HomePage()
    {
        InitializeComponent();

    }

    // Muestra una ventana de alerta con el texto indicado. Usado mucho para depurar y para mostrar excepciones capturadas.
    async Task ShowMessage(string text)
    {
        await App.Current.MainPage.DisplayAlert("Whoopsie", text, "Close");

    }

    // Muestra un array de bytes en una ventana de alerta. Usado para depurar y ver cómo se ven los píxeles de la imagen.
    // Usado tanto antes como después de la normalización
    public async void ShowArray(byte[] pixeles)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < pixeles.Length; i++)
        {
            sb.Append(pixeles[i]);
            sb.Append(" ");
        }
        await ShowMessage(sb.ToString());
    }



    // Evento producido al pulsar el botón de predicción.
    // Se encarga de obtener la imagen del canvas, añadirle padding, convertirla a un array de bytes y pasárselo al script de Python.
    // Finalmente, muestra el resultado de la predicción en la etiqueta correspondiente.
    private async void OnPredictClicked(object sender, EventArgs e)
    {
        try
        {
            int requiredPixels = 28;
            // Obtener la imagen del canvas
            using var stream = await drawingCanvas.GetImageStream(requiredPixels, requiredPixels);

            // Pasar la imagen a un MemoryStream para poder trabajar con ella
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            // Resetear la posición del stream para poder leerlo desde el principio y que no de excepción
            stream.Position = 0;
            memoryStream.Position = 0;

            // Guardar la imagen en el disco para poder verla
            await System.IO.File.WriteAllBytesAsync(dataPath + "imagen.png", memoryStream.ToArray());

            // Creación de una variable con la imagen original para añadirle padding
            using var originalImage = System.Drawing.Image.FromStream(memoryStream);

            //Nuevos valores para el padding
            int newWidth = originalImage.Width + 200; // 200 píxeles de padding a cada lado
            int newHeight = originalImage.Height + 200; // 200 píxeles de padding arriba y abajo
            using var paddedImage = new Bitmap(newWidth, newHeight);

            // Dibujar la imagen original en la nueva imagen centrada para que el padding quede en las 4 direcciones
            using (Graphics g = Graphics.FromImage(paddedImage))
            {
                g.Clear(System.Drawing.Color.LightGray); // Establecer el color de fondo en gris (ya que es el mismo con el que es guardaba la original)
                int x = (newWidth - originalImage.Width) / 2; // Ponerla en el centro
                int y = (newHeight - originalImage.Height) / 2;
                g.DrawImage(originalImage, x, y); // Dibujar la imagen original sobre la nueva
            }
            // Guardado de la imagen con padding
            paddedImage.Save(dataPath + "imagen_con_padding.png", System.Drawing.Imaging.ImageFormat.Png);


            // Pasar la imagen con padding a un MemoryStream para poder trabajar con ella
            using (var fileStream = new FileStream(dataPath + "imagen_con_padding.png", FileMode.Open))
            {
                using (var ms = new MemoryStream())
                {

                    await fileStream.CopyToAsync(ms);
                    fileStream.Position = 0;
                    ms.Position = 0;

                    // Convertir la imagen a un array de bytes
                    byte[] pixeles = Convert(ms);
                    //ShowArray(pixeles); Utilizado para depurar y ver cómo se ven los píxeles de la imagen antes de la normalización.

                    for (int i = 0; i < pixeles.Length; i++)
                    {
                        if (pixeles[i] < 200) // Valores inferiores a 200 son los de color negro, el resto son del fondo gris
                        {
                            pixeles[i] = 1; // Negro
                        }
                        else
                        {
                            pixeles[i] = 0; //Blanco
                        }
                    }

                    // Escribir los bytes de texto en un archivo
                    string pixelString = string.Join(" ", pixeles);
                    string filePath = dataPath + "bytes.txt"; //Útil para depurar y ver cómo funciona el programa.
                    File.WriteAllText(filePath, pixelString);

                    // Pasar el array a otro de formato float para evitar errores de compatibilidad con la predicción del modelo Python
                    float[] floats = new float[pixeles.Length];
                    for (int i = 0; i < pixeles.Length; i++)
                    {
                        floats[i] = pixeles[i];
                    }

                    // Ejcución del script pasando como argumento el array normalizado
                    await ExecuteScript(floats);
                }
            }
        }

        catch (System.Runtime.InteropServices.ExternalException ex) //El usuario presiona el botón sin terminar la predicción
        {
            ShowMessage("Please wait until the generation has finished.");
        } catch (System.InvalidOperationException ex) //El usuario dibuja fuera del canvas
        {
            ShowMessage("Please make sure you don't paint outside the canvas or that you have paint something on it!");
        } catch (System.IO.DirectoryNotFoundException) { //Directorio data no encontrado
            ShowMessage("The data folder is missing. Please make sure you have the data folder in the AppX directory. Are you running in Debug mode?");
        } catch (Exception ex) //Otros
        {
            ShowMessage("Unknown error." + ex.ToString());
        }

    }


    // Convierte la imagen a un array de bytes tras redimensionarse a 28x28 píxeles y convertirse a escala de grises
    private static byte[] Convert(MemoryStream imageStream)
    {
        int targetWidth = 28;
        int targetHeight = 28;
        // Redimensionar la imagen a 28x28 píxeles
        using (var skBitmap = SKBitmap.Decode(imageStream))
        {
            using (var resizedBitmap = skBitmap.Resize(new SKImageInfo(targetWidth, targetHeight), SKFilterQuality.High))
            {
                // Crear un array de bytes para guardar los píxeles de la imagen
                byte[] pixeles = new byte[targetWidth * targetHeight];

                // Recorrer la imagen redimensionada
                for (int y = 0; y < targetHeight; y++)
                {
                    for (int x = 0; x < targetWidth; x++)
                    {
                        // Obtiene el color del píxel en el formato RGB
                        SKColor color;
                        if (x < resizedBitmap.Width && y < resizedBitmap.Height) //Si el valor está dentro de las posiciones de la imagen...
                        {
                            color = resizedBitmap.GetPixel(x, y); // obtiene el color del píxel
                        }
                        else
                        {
                            // Si estamos fuera de las dimensiones de la imagen redimensionada, asignamos blanco
                            color = SKColors.White;
                        }

                        // Calcula el valor de intensidad (0 para negro, 255 para blanco)
                        byte intensity = (byte)((color.Red + color.Green + color.Blue) / 3);

                        // Calcula la intensidad del pixel como la media de los componentes de color RGB
                        pixeles[y * targetWidth + x] = intensity;
                    }
                }
                return pixeles;
            }

        }
    }


    // Ejecuta el script con el array normalizado pasado por parámetro
    private async Task ExecuteScript(float[] arr)
    {
        // Comienza la predicción; se muestra el indicador de carga
        loadingIndicator.IsRunning = true;
        string reemplazar = "";

        try
        {
            // Creación de un hilo para no congelar la ejecución del programa mientras se realia la predicción
            await Task.Run(() =>
            {
                // Serializar el array en JSON para pasarlo por argumento sin problemas
                string serializedArray = JsonConvert.SerializeObject(arr);

                //Script que se cargará
                string pythonScript = dataPath + "cargarModelo.py";

                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = "python3.11.exe"; // Comando para ejecutar Python
                start.Arguments = $"{pythonScript} {serializedArray}"; // Pasar el array serializado como argumento al script Python

                start.UseShellExecute = false; // No usar shell para la ejecución
                start.RedirectStandardOutput = true; // Redireccionar la salida para capturarla con una regex
                start.CreateNoWindow = true; // No crear ventana

                // Inicia el proceso
                using (Process process = Process.Start(start))
                {
                    // Leer toda la salida del script
                    string output = process.StandardOutput.ReadToEnd();

                    // Esperar a que el proceso Python termine
                    process.WaitForExit();

                    // Aplicamos una regex a la salida para filtrar por el print que nos interesa (Predicción final: N)
                    string pattern = @": [0-9]";

                    MatchCollection matches = Regex.Matches(output, pattern);
                    reemplazar = matches[1].Value.Replace(":", "").ToString(); //Reemplazar los 2 puntos de la regex por carácter vacío

                }
            });
            lblPrediction.Text = reemplazar; // Mostrar el resultado de la predicción en la etiqueta correspondiente

            loadingIndicator.IsRunning = false; // Parar el indicador de carga
        }
        catch (System.ArgumentOutOfRangeException) //El script no ha devuelto el resultado esperado
        {
            ShowMessage("Looks like the Python script exited with an error. Make sure you have all the needed libs installed (find them at requirements.txt) and that you are running a proper version of Python (3.11). Do you have your Python 3.11 version on system's path?");
        }
        catch (Exception e) //Otros
        {
            await ShowMessage(e.ToString());

        }

    }

}