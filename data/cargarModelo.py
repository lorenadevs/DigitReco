# -*- coding: utf-8 -*-

from __future__ import absolute_import, division, print_function, unicode_literals

import tensorflow as tf

import numpy as np
import sys
import json
import os

# ruta = 'C:/Users/loren/Documents/MAUI/DigitRecoUI/DigitRecoUI/bin/Debug/net8.0-windows10.0.19041.0/win10-x64/AppX/data/modelo.h5'
ruta_actual = os.path.abspath(os.path.dirname(__file__))
ruta = os.path.join(ruta_actual, "modelo.h5")


""" Función encargada de pasarle píxeles de una imagen contenidos en bytes a un modelo para realizar una predicción.
"""
def predecir_exc(arr):
    try:
        modelo = tf.keras.models.load_model(ruta) #Cargar el modelo entrenado .h5

        #Darle formato al array con Numpy para trabajar con él
        arr = np.array(arr)
        arr = arr.reshape(1, 28, 28, 1)
        
        # Hacer la predicción utilizando el modelo
        prediction_values = modelo.predict(arr, batch_size=1)

        # Muestra los 3 números con mayor probabilidad; no utilizado al final pero útil de cara a futuro
        # top_indices = np.argsort(prediction_values)[0][::-1]
        # top_probabilities = prediction_values[0][top_indices[:3]]
        # top_classes = top_indices[:3]

        # for _, (class_idx, probability) in enumerate(zip(top_classes, top_probabilities), 1):
        #     print(f"{class_idx} con {probability}%")

        prediction = str(np.argmax(prediction_values)) #Número que mayor certeza tiene

        print("Predicción final:", prediction) #Texto a cortar con una regex desde C#


        return prediction
    
    except Exception as e:
        # Capturar cualquier excepción que ocurra y mostrar un mensaje de error
        print("Error durante la predicción:", e)
        return None


def main():
    #Coger el array en JSON por argumentos del script
    serialized_array = sys.argv[1]

    # Deserializar el array
    arr = json.loads(serialized_array)
    
    # Llamar a la función de predicción con el array deserializado
    predecir_exc(arr)



if __name__ == "__main__":
    main()