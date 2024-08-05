from __future__ import absolute_import, division, print_function, unicode_literals

import tensorflow as tf

import tensorflow_datasets as tfds

import math

ruta = 'modelo.h5' #Ruta en la que guardar el modelo


#Cargar dataset completo de MNIST
dataset, metadata = tfds.load('mnist', as_supervised=True, with_info=True)

#División del dataset en datos para entreno y para pruebas
train_dataset, test_dataset = dataset['train'], dataset['test']

#Metadatos
num_train_examples = metadata.splits['train'].num_examples
num_test_examples = metadata.splits['test'].num_examples 

"""Normalización de valores de cada neurona desde 0 a 255 a
    0 y 1 para saber cuándo es blanco y cuándo negro.
"""
def normalize(images, labels):
    images = tf.cast(images, tf.float32)
    images /= 255
    return images, labels


train_dataset = train_dataset.map(normalize)
test_dataset = test_dataset.map(normalize)

#Estructura de la red; elección de red secuencial (RNN)
model = tf.keras.Sequential([
	tf.keras.layers.Flatten(input_shape=(28,28,1)), # 28 x 28 píxeles cada imagen
	tf.keras.layers.Dense(64, activation=tf.nn.relu), #Capa oculta 1
	tf.keras.layers.Dense(64, activation=tf.nn.relu), #Capa oculta 2
	tf.keras.layers.Dense(10, activation=tf.nn.softmax) #softmax es obligatorio para redes de clasificación
])

#Indicar las funciones a utilizar
model.compile(
	optimizer='adam',
	loss='sparse_categorical_crossentropy',
	metrics=['accuracy']
)

#Aprendizaje por lotes de 64 cada lote
BATCHSIZE = 64
train_dataset = train_dataset.repeat().shuffle(num_train_examples).batch(BATCHSIZE) #Aleatorizar los datos
test_dataset = test_dataset.batch(BATCHSIZE)

#Realizar el aprendizaje
model.fit(
	train_dataset, epochs=50,
	steps_per_epoch=math.ceil(num_train_examples/BATCHSIZE)
)


model.save(ruta)
