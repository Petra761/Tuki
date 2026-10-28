-- Consultas de solo lectura para ejecutar en el SQL Editor de Supabase.
-- No devuelven datos personales; agrupan únicamente conteos.

-- Total histórico de inscripciones y cuántas siguen en estado inscrito.
select
  count(*) as total_inscripciones,
  count(*) filter (where estado = 'inscrito') as total_estado_inscrito
from public.interesados;

-- Inscripciones por día de creación, según la zona horaria de la sesión SQL.
select
  creado_en::date as dia,
  count(*) as total_inscripciones
from public.interesados
group by creado_en::date
order by dia desc;

-- Inscripciones por plan de interés.
select
  plan_interes,
  count(*) as total_inscripciones
from public.interesados
group by plan_interes
order by total_inscripciones desc, plan_interes;

-- Inscripciones por estado actual.
select
  estado,
  count(*) as total_inscripciones
from public.interesados
group by estado
order by estado;
