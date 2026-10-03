# Diseño de agentes

## Objetivo
Mantener el nivel de disciplina de XaurixFX y adaptarlo al dominio BillingRD sin copiar reglas de negocio.

## Mejoras introducidas
1. Clasificación LOW/MEDIUM/HIGH/CRITICAL antes de delegar.
2. Separación de requirements, QA y reviewer.
3. Perfiles de dominio: billing, inventory, DGII y offline.
4. Revisión independiente obligatoria para cambios CRITICAL.
5. Una tarea LOW no activa coordinator + todos los perfiles.

## Contexto mínimo
AGENTS.md es la entrada. .agents/index.md decide qué leer después.
Un especialista recibe requisito, contrato, alcance, aceptación y checks concretos.
No recibe historiales irrelevantes.

## Evaluación futura
Para tareas comparables registrar cuando aporte valor: perfiles usados, archivos leídos,
reintentos, tests, fallos/reaperturas y aceptación. Optimizar solo si conserva controles críticos.

## Restricción
Los Markdown no crean agentes nativos ni cambian modelo/runtime; definen el comportamiento esperado.
