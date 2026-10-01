"""Reproduce the mathematical examples in Difficulty Formula.md. Not a Unity analyzer."""
from itertools import permutations,combinations
from math import perm,log2,factorial
from collections import Counter
import random,json
from pathlib import Path
REF=log2(perm(16,12))

def food_domain(seats,foods,conditions):
    def adjacent(p,q):return abs(p[0]-q[0])+abs(p[1]-q[1])==1
    allowed=[]
    for i,s in enumerate(seats):
        nearby={f for pos,f in foods.items() if adjacent(s,pos)}
        if all(((bool(nearby) if target=='Any' else target in nearby)==(kind=='Like'))
               for kind,target in conditions):allowed.append(i)
    return allowed

def stats(lv):
    seats,foods,people=lv['seats'],lv['foods'],lv['people']
    neighbors=[{j for j,t in enumerate(seats) if abs(s[0]-t[0])+abs(s[1]-t[1])==1} for s in seats]
    domains=[food_domain(seats,foods,p['food']) for p in people]
    def flags(state,p):
        if p not in state[:len(seats)]:return []
        s=state.index(p);traits={people[state[j]]['trait'] for j in neighbors[s] if state[j]>=0}
        nearby={f for pos,f in foods.items() if abs(seats[s][0]-pos[0])+abs(seats[s][1]-pos[1])==1}
        return [((bool(nearby) if target=='Any' else target in nearby)==(kind=='Like')) for kind,target in people[p]['food']]+[((target in traits)==(kind=='Like')) for kind,target in people[p]['person']]
    n=len(people);S=len(seats);Tu=K=0
    for destinations in permutations(range(S),n):
        if not all(destinations[p] in domains[p] for p in range(n)):continue
        Tu+=1
        state=[-1]*S
        for p,s in enumerate(destinations):state[s]=p
        if all(all(flags(state,p)) for p in range(n)):K+=1
    def sim(eps,rng,Mmax):
        state=tuple([-1]*S+list(range(n)));visits=Counter({state:1})
        for step in range(1,3*Mmax+1):
            moves=[]
            for a,b in combinations(range(S+n),2):
                if state[a]<0 and state[b]<0:continue
                nxt=list(state);nxt[a],nxt[b]=nxt[b],nxt[a];nxt=tuple(nxt)
                placed=[p for p in range(n) if p in nxt[:S]]
                fs=[flags(nxt,p) for p in placed]
                key=(sum(all(f) for f in fs),sum(sum(f) for f in fs),len(placed))
                moves.append((key,-visits[nxt],-a,-b,nxt))
            if rng.random()<eps:nxt=rng.choice(moves)[-1]
            else:nxt=max(moves)[-1]
            state=nxt;visits[state]+=1
            if all(p in state[:S] and all(flags(state,p)) for p in range(n)):return step
        return None
    out=dict(N=n,S=S,W=n,total=perm(S,n),domains=domains,Tu=Tu,K=K)
    if Tu and K:
        out.update(Iu=log2(perm(S,n)/Tu),Ir=log2(Tu/K),Iref=REF)
        profiles=[]
        for eps in (0,.2,.4):
            runs=1 if eps==0 else 2000
            rng=random.Random(20260930+round(eps*100))
            times=[sim(eps,rng,lv['Mmax']) for _ in range(runs)]
            rates={str(M):sum(t is not None and t<=M for t in times)/runs for M in lv['budgets']}
            profiles.append(dict(epsilon=eps,runs=runs,win=rates))
        out['profiles']=profiles
        out['L']={str(M):1-sum(p['win'][str(M)] for p in profiles)/3 for M in lv['budgets']}
    return out

people=lambda trait,food=(),person=():dict(trait=trait,food=list(food),person=list(person))
levels={
 'Food':dict(seats=[(0,0),(2,0),(4,0)],foods={(1,0):'H'},people=[people('Cool',[('Like','H')]),people('Dirty',[('Hate','Any')])],Mmax=4,budgets=[2,3,4]),
 'LikePair':dict(seats=[(0,0),(1,0),(2,0)],foods={},people=[people('Cool',person=[('Like','Sick')]),people('Sick',person=[('Like','Cool')])],Mmax=4,budgets=[2,3,4]),
 'HatePair':dict(seats=[(0,0),(1,0),(2,0)],foods={},people=[people('Cool',person=[('Hate','Sick')]),people('Sick',person=[('Hate','Cool')])],Mmax=4,budgets=[2,3,4]),
 'FoodIntersection':dict(seats=[(0,0),(2,0),(4,0)],foods={(1,0):'H',(2,1):'F'},people=[people('Cool',[('Like','H'),('Hate','F')])],Mmax=3,budgets=[1,2,3]),
 'Branching':dict(seats=[(0,0),(1,0),(2,0),(3,0)],foods={},people=[people('Cool',person=[('Like','Sick'),('Like','Dirty')]),people('Sick'),people('Dirty')],Mmax=5,budgets=[3,4,5]),
 'CapacityConflict':dict(seats=[(0,0),(3,0)],foods={(1,0):'H'},people=[people('Cool',[('Like','H')]),people('Dirty',[('Like','H')])],Mmax=3,budgets=[2,3]),
 'AnyContradiction':dict(seats=[(0,0),(2,0),(4,0)],foods={(1,0):'H'},people=[people('Cool',[('Like','H'),('Hate','Any')])],Mmax=3,budgets=[1,2,3])
}
result={name:stats(level) for name,level in levels.items()}
assert result['Food']['Tu']==result['Food']['K']==2
assert result['LikePair']['Tu']==6 and result['LikePair']['K']==4
assert result['HatePair']['K']==2
assert result['FoodIntersection']['domains']==[[0]]
assert result['Branching']['K']==4
assert result['CapacityConflict']['Tu']==0
assert result['AnyContradiction']['Tu']==0
for r in result.values():
 if r['Tu'] and r['K']:
  assert abs(r['Iu']+r['Ir']-log2(r['total']/r['K']))<1e-12
  assert list(r['L'].values())==sorted(r['L'].values(),reverse=True)

large=dict(total=perm(16,12),Tu=factorial(6)**2,Iref=REF)
large['Iu']=log2(large['total']/large['Tu']);large['FoodContribution']=10*large['Iu']/REF

result['LargeFood']=large
for name,seats,good in [('HatePair',range(3),{0,2}),('Branching',range(4),{1,2})]:
 bs=[]
 for order in permutations(seats):
  b=0
  for seat in order:
   if seat in good:break
   b+=1
  bs.append(b)
 mean=sum(bs)/len(bs)
 expected=1/3 if name=='HatePair' else 2/3
 assert abs(mean-expected)<1e-12
 result[name]['b_mean_exact_for_documented_search']=mean
 result[name]['Rhat']=log2(1+mean)/log2(10001)
result['Branching']['Rhat']=log2(1+2/3)/log2(10001)
for name,r in result.items():
 if 'profiles' not in r: continue
 r['Rhat']=r.get('Rhat',0)
 r['score']={str(M):100*(.10*r['Iu']/REF+.40*r['Ir']/REF+.15*r['Rhat']+.35*L) for M,L in r['L'].items()}
 print(name,'Rhat=',r['Rhat'],'scores=',r['score'])
Path(__file__).with_name('difficulty-formula-verification-results.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')

# Cross-check unary bitmask DP against exhaustive labelled assignments.
for name, r in result.items():
    if 'domains' not in r:
        continue
    dp = {0: 1}
    for domain in r['domains']:
        next_dp = {}
        for mask, count in dp.items():
            for seat in domain:
                if not mask & (1 << seat):
                    child = mask | (1 << seat)
                    next_dp[child] = next_dp.get(child, 0) + count
        dp = next_dp
    assert sum(dp.values()) == r['Tu'], name
print('PASS: seven exhaustive cases, unary DP, information identity, Move monotonicity, and analytic backtrack expectations.')
