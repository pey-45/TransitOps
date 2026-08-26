# Memoria TFG - TransitOps

Esta carpeta contiene la memoria del Trabajo Fin de Grado de Pablo Manzanares López:

`Diseño y desarrollo de una aplicación de gestión de transportes: ciclo de vida completo del software`.

El fichero principal es `memoria_tfg.tex`. La estructura combina capítulos por fase del ciclo de vida con un capítulo de desarrollo iterativo que conserva la crónica sprint a sprint. Los siete primeros incrementos están ejecutados y documentados con su evidencia, incluidos el endurecimiento, las pruebas de sistema y el despliegue accesible del séptimo; el octavo es el cierre de documentación y defensa al que corresponde esta memoria.

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

## Plantilla

La plantilla base es el modelo oficial de memoria de TFG de la Facultad de Informática de la Universidade da Coruña, del que se conserva una copia íntegra, junto con los recursos institucionales, en `archive/cloud-phase/tfg/memoria/`. Los créditos y la licencia original están en `CREDITS` y `COPYING`.

`estilo_tfg.sty` se aparta de esa copia en dos puntos, ambos comentados en el propio fichero:

1. El renombrado de «Cuadro» a «Tabla» y de «Índice de cuadros» a «Índice de tablas» deja de estar condicionado a la macro `\renomearcadros`, que la plantilla nunca definía, y se aplica siempre en la rama de español. Las referencias del texto dicen «la Tabla N», de modo que la denominación por defecto de babel-spanish las contradecía.
2. Los rótulos de página pasan de `\small` a `\footnotesize`. A `\small`, los dos pares de capítulo y apartado más largos no caben en `\headwidth` y `fancyhdr` los solapa sin emitir aviso de caja desbordada.

Un `diff` contra la copia archivada muestra exactamente esos dos cambios y ningún otro.
