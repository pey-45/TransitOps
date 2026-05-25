# Memoria TFG - TransitOps

Esta carpeta contiene la memoria del Trabajo Fin de Grado de Pablo Manzanares López:

`Diseño, despliegue y operación de una plataforma cloud para la gestión de transportes en AWS mediante infraestructura como código y prácticas DevOps`.

El fichero principal es `memoria_tfg.tex` y el PDF de entrega generado es `memoria_tfg.pdf`.

## Estructura

1. `contido/`: capítulos principales de la memoria.
2. `anexos/`: material complementario, trazabilidad y procedimientos operativos.
3. `bibliografia/`: bibliografía, acrónimos y glosario.
4. `imaxes/`: figuras, capturas y evidencias incorporadas.
5. `portada/`: portada, resumen y palabras clave.

## Generación del PDF

La memoria se compila con XeLaTeX mediante `latexmk`:

```powershell
latexmk -xelatex memoria_tfg.tex
```

Los ficheros auxiliares de compilación no forman parte de la entrega y pueden limpiarse con:

```powershell
latexmk -xelatex -c
```

La plantilla base procede del modelo oficial de memoria de TFG de la Facultad de Informática de la Universidade da Coruña. Los créditos y la licencia original se conservan en `CREDITS` y `COPYING`.
