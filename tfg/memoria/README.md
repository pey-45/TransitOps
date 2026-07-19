# Memoria TFG - TransitOps

Esta carpeta contiene la memoria del Trabajo Fin de Grado de Pablo Manzanares López:

`Diseño y desarrollo de una aplicación de gestión de transportes: ciclo de vida completo del software`.

El fichero principal es `memoria_tfg.tex`. La estructura combina capítulos por fase del ciclo de vida con un capítulo de desarrollo iterativo; el Sprint 1 contiene resultados reales y S2--S8 mantienen placeholders explícitos hasta su ejecución.

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

La plantilla, los recursos institucionales y sus ficheros de licencia proceden exclusivamente de la plantilla UDC-FIC conservada en `archive/cloud-phase/tfg/memoria/`.

La plantilla base procede del modelo oficial de memoria de TFG de la Facultad de Informática de la Universidade da Coruña. Los créditos y la licencia original se conservan en `CREDITS` y `COPYING`.
