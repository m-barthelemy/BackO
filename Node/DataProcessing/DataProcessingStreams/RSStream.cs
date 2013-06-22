#define MUTE
// implementation from thesis : http://sidewords.files.wordpress.com/2007/12/thesis.pdf
using System;
using System.Collections.Generic;
using System.Text;
namespace SoftDecoding
{
	
	
	public struct FFE
{
internal static FiniteField DEFAULT_FIELD;
internal readonly int value;
internal FFE(int value) {
this.value = value;
}
public override String ToString() {
if (DEFAULT_FIELD.n == 1)
return this.value.ToString();
else
{
String ans = "";
int[] coefs = DEFAULT_FIELD.AsCoefs(value);
for (int i = 0; i < coefs.Length; i++)
ans += coefs[i];
return ans;
/*
if (this.value == 0)
return "X";
for (int i = 0; i < DEFAULT_FIELD.q; i++)
{
int power = (DEFAULT_FIELD[2] ^ i).value;
if (this.value == power)
return i.ToString();
}
throw new Exception("Field element is not the power of the generator element.");
*/
}
}
public static FFE operator +(FFE a, FFE b) {
return DEFAULT_FIELD.Add(a, b);
}
public static FFE operator -(FFE a) {
return DEFAULT_FIELD.Opposite(a);
}
public static FFE operator -(FFE a, FFE b) {
return DEFAULT_FIELD.Substract(a, b);
}
public static FFE operator *(FFE a, FFE b) {
return DEFAULT_FIELD.Multiply(a, b);
}
public static FFE operator /(FFE a, FFE b) {
return DEFAULT_FIELD.Divide(a, b);
}
public static FFE operator ^(FFE a, int exp) {
return DEFAULT_FIELD.Power(a, exp);
}
public static bool operator ==(FFE a, FFE b) {
return a.value == b.value;
}
public static bool operator !=(FFE a, FFE b){
return a.value != b.value;
}
public static String VectorToString(FFE[] vector)
{
String ans = "";
for (int i = 0; i < vector.Length; i++)
ans += vector[i].ToString() + " ";
return ans;
}
}
//}


public abstract class FiniteField
{
public readonly int p;
public readonly int n;
public readonly int q;
public readonly FFE[] elements;
protected FiniteField(int p, int n)
{
this.p = p;
this.n = n;
this.q = (int) Math.Pow(p, n);
this.elements = new FFE[q];
for (int i = 0; i < q; i++)
elements[i] = new FFE(i);
FFE.DEFAULT_FIELD = this;
}
public abstract FFE Add(FFE a, FFE b);
public abstract FFE Substract(FFE a, FFE b);
public abstract FFE Multiply(FFE a, FFE b);
public abstract FFE Divide(FFE a, FFE b);
public abstract FFE Opposite(FFE a);
public abstract FFE Power(FFE a, int exp);
public FFE this[int value]
{
get
{
return this.elements[value];
}
}
public FFE this[int[] coefs]
{
get
{
return this[AsValue(coefs)];
}
}
public int[] AsCoefs(FFE a)
{
return AsCoefs(a.value);
}
public int[] AsCoefs(int value)
{
int[] coefs = new int[n];
int ppow = 1;
for (int i = 0; i < n; i++)
{
coefs[i] = (value % (p*ppow))/ppow;
ppow *= p;
}
return coefs;
}
public int AsValue(int[] coefs)
{
if (coefs.Length != n)
throw new ArgumentException("Wrong number of coefficients. Should be " + n + " instead of " + coefs.Length + ".");
int value = 0;
int ppow = 1;
for (int i = 0; i < n; i++)
{
value += coefs[i] * ppow;
ppow *= p;
}
return ((value % q) + q) % q;
}
}
	
	
struct Polynomial
{
private FFE[,] values;
public FFE this[int i, int j]
{
get
{
if (i < values.GetLength(0) & j < values.GetLength(1))
return values[i, j];
else
return FFE.DEFAULT_FIELD[0];
}
set
{
126if (i < values.GetLength(0) & j < values.GetLength(1))
values[i, j] = value;
else
{
FFE[,] old = values;
values = new FFE[Math.Max(i, old.GetLength(0)), Math.Max(j, old.GetLength(1))];
for (int ii = 0; ii < old.GetLength(0); ii++)
for (int jj = 0; jj < old.GetLength(1); jj++)
values[ii, jj] = old[ii, jj];
values[i, j] = value;
}
}
}
public Polynomial(int[,] values)
{
this.values = new FFE[values.GetLength(0), values.GetLength(1)];
for (int i = 0; i < values.GetLength(0); i++)
for (int j = 0; j < values.GetLength(1); j++)
this.values[i, j] = FFE.DEFAULT_FIELD[values[i,j]];
}
public Polynomial(FFE[,] values)
{
this.values = values;
}
public Polynomial(FFE[] values)
{
this.values = new FFE[values.Length, 1];
for (int i = 0; i < values.Length; i++)
this.values[i,0] = values[i];
}
public static Polynomial GetX()
{
return new Polynomial(new FFE[,] { { FFE.DEFAULT_FIELD[0] }, { FFE.DEFAULT_FIELD[1] } });
}
public static Polynomial GetY()
{
return new Polynomial(new FFE[,] { { FFE.DEFAULT_FIELD[0], FFE.DEFAULT_FIELD[1] } });
}
public int GetMaxDegX() {
return values.GetLength(0)-1;
}
public int GetDegreeX()
{
for (int i = this.GetMaxDegX(); i >= 0; i--)
for (int j = 0; j <= this.GetMaxDegY(); j++)
if (values[i, j] != FFE.DEFAULT_FIELD[0])
return i;
return 0;
}
public int GetMaxDegY(){
return values.GetLength(1)-1;
}
public Polynomial(int maxDegX, int maxDegY)
{
this.values = new FFE[maxDegX+1, maxDegY+1];
}
public static Polynomial operator +(Polynomial P, Polynomial Q)
{
Polynomial res = new Polynomial(
Math.Max(P.GetMaxDegX(), Q.GetMaxDegX()),
Math.Max(P.GetMaxDegY(), Q.GetMaxDegY()));
for (int i = 0; i <= res.GetMaxDegX(); i++)
for (int j = 0; j <= res.GetMaxDegY(); j++)
res[i, j] = P[i, j] + Q[i, j];
return res;
}
public static Polynomial operator -(Polynomial P, Polynomial Q)
{
Polynomial res = new Polynomial(
Math.Max(P.GetMaxDegX(), Q.GetMaxDegX()),
Math.Max(P.GetMaxDegY(), Q.GetMaxDegY()));
for (int i = 0; i <= res.GetMaxDegX(); i++)
for (int j = 0; j <= res.GetMaxDegY(); j++)
res[i, j] = P[i, j] - Q[i, j];
return res;
}
public static Polynomial operator *(Polynomial P, Polynomial Q)
{
Polynomial res = new Polynomial(P.GetDegreeX() + Q.GetDegreeX(), P.GetMaxDegY() + Q.GetMaxDegY());
for (int pi = 0; pi <= P.GetDegreeX(); pi++)
for (int pj = 0; pj <= P.GetMaxDegY(); pj++)
if(P[pi,pj] != FFE.DEFAULT_FIELD[0])
for (int qi = 0; qi <= Q.GetDegreeX(); qi++)
for (int qj = 0; qj <= Q.GetMaxDegY(); qj++)
res[pi+qi, pj+qj] += P[pi, pj] * Q[qi, qj];
return res;
}
public static Polynomial operator ^(Polynomial P, int exp)
{
if (P == GetX())
{
Polynomial res = new Polynomial(exp, 0);
res[exp,0] = FFE.DEFAULT_FIELD[1];
return res;
}
else if (P == GetY())
{
Polynomial res = new Polynomial(0, exp);
res[0, exp] = FFE.DEFAULT_FIELD[1];
return res;
}
else
{
Polynomial ans = FFE.DEFAULT_FIELD[1];
for (int i = 0; i < exp; i++)
ans *= P;
return ans;
}
}
// implicit conversion from an FFE to a polynomial
public static implicit operator Polynomial(FFE value) {
return new Polynomial(new FFE[,] { { value } });
}
public FFE Evaluate(FFE xVal, FFE yVal)
{
FFE res = FFE.DEFAULT_FIELD[0];
for (int i = 0; i <= GetMaxDegX(); i++)
for (int j = 0; j <= GetMaxDegY(); j++)
res += this.values[i,j]*(xVal^i)*(yVal^j);
return res;
}
public override string ToString()
{
String ans = "";
FFE zero = FFE.DEFAULT_FIELD[0];
for (int j = 0; j <= GetMaxDegY(); j++)
for (int i = 0; i <= GetMaxDegX(); i++)
if (this[i, j] != zero)
ans += this[i, j] + (i == 0 ? "" : "x^" + i) + (j == 0 ? "" : "y^" + j) + " + ";
if (ans.Length > 0)
return ans.Substring(0, ans.Length - 2);
else
return "0";
}
public static bool operator ==(Polynomial P, Polynomial Q)
{
return !(P != Q);
}
public static bool operator !=(Polynomial P, Polynomial Q)
{
int maxDegX = (int) Math.Max(P.GetMaxDegX(), Q.GetMaxDegX());
int maxDegY = (int) Math.Max(P.GetMaxDegY(), Q.GetMaxDegY());
for (int i = 0; i <= maxDegX; i++)
for (int j = 0; j <= maxDegY; j++)
if (P[i, j] != Q[i, j])
return true;
return false;
}
}
	
	

class RSEncoder : Encoder
{
public readonly int n;
public readonly int k;
public readonly FFE[] alphas;
internal RSEncoder(int k, FFE[] alphas)
{
this.n = alphas.Length;
this.k = k;
this.alphas = alphas;
}
public static FFE[] GetDefautLocation(FiniteField Fq, int n)
{
if (n > Fq.q)
throw new ArgumentException("More locations than field elements is not allowed. You must ensure n <= q.");
FFE[] alphas = new FFE[n];
if (n < Fq.q)
for (int i = 0; i < n; i++)
alphas[i] = Fq[i + 1];
else // n == q
for (int i = 0; i < n; i++)
alphas[i] = Fq[i];
return alphas;
}
public FFE[] Encode(FFE[] message)
{
Polynomial f = new Polynomial(message);
FFE[] x = new FFE[n];
for (int i = 0; i < n; i++)
x[i] = f.Evaluate(alphas[i], FFE.DEFAULT_FIELD[0]);
return x;
}
}



class GaoDecoder : Decoder
{
protected FiniteField Fq;
protected int k;
protected int n;
protected FFE[] alphas;
public GaoDecoder(FiniteField Fq, FFE[] alphas, int k)
{
this.Fq = Fq;
this.alphas = alphas;
this.n = alphas.Length;
this.k = k;
}
FFE[] Decoder.Decode(double[,] RM)
{
FFE[] y = new FFE[n];
for (int i = 0; i < n; i++)
{
int maxJ = 0;
double max = 0;
for (int j = 0; j < Fq.q; j++)
{
if (RM[i, j] > max)
{
max = RM[i, j];
maxJ = j;
}
}
y[i] = Fq[maxJ];
}
return this.Decode(y);
}
private FFE[] Decode(FFE[] y)
{
Polynomial[] p_i = new Polynomial[3];
Polynomial[] v_i = new Polynomial[3] { Fq[0], Fq[0], Fq[1] };
Polynomial x = Polynomial.GetX();
// p_1 = prod_j (x - alpha_j)
p_i[1] = Fq[1];
for (int i = 0; i < n; i++)
p_i[1] *= (x - alphas[i]);
// p_2 = sum y_i prod (x - alpha_j)/(alpha_i - alpha_j)
p_i[2] = Fq[0];
for (int i = 0; i < n; i++)
{
Polynomial fact = y[i];
for (int j = 0; j < n; j++)
if (j != i)
fact *= Fq[1]/(alphas[i] - alphas[j])*(x - alphas[j]);
p_i[2] += fact;
}
if (p_i[2].GetDegreeX() < k)
{
FFE[] hatx = new FFE[n];
for (int i = 0; i < n; i++)
hatx[i] = p_i[2].Evaluate(alphas[i], Fq[0]);
return hatx;
}
// the Euclidean division begins...
Polynomial[] QR;
do
{
p_i[0] = p_i[1];
p_i[1] = p_i[2];
v_i[0] = v_i[1];
v_i[1] = v_i[2];
QR = GetQuotientRemainder(p_i[0], p_i[1]);
p_i[2] = QR[1];
v_i[2] = -Fq[1] * QR[0] * v_i[1] + v_i[0];
}
while(2 * p_i[2].GetDegreeX() >= n+k);
//p_i[2] = f * v[2] + r
QR = GetQuotientRemainder(p_i[2], v_i[2]);
if (QR[1] == Fq[0])
{
// Success
FFE[] hatx = new FFE[n];
for (int i = 0; i < n; i++)
hatx[i] = QR[0].Evaluate(alphas[i], Fq[0]);
return hatx;
}
else
{
// Failure
return new FFE[n];
}
}
private Polynomial[] GetQuotientRemainder(Polynomial dividend, Polynomial divisor)
{
if (divisor == Fq[0])
throw new DivideByZeroException();
if(dividend.GetDegreeX() < divisor.GetDegreeX())
throw new ArgumentException("The degree of the dividend cannot be less than the degree of the divisor.");
Polynomial remainder = Fq[1] * dividend; // to make a "by value copy" of the dividend
Polynomial quotient = new Polynomial(0,0);
Polynomial x = Polynomial.GetX();
int degDiv = divisor.GetDegreeX();
FFE leadingCoeficient = divisor[degDiv,0];
for (int deg = dividend.GetDegreeX(); deg >= degDiv; deg--)
{
FFE coef = remainder[deg, 0] / leadingCoeficient;
quotient += coef * (x ^ (deg - degDiv));
remainder -= coef * divisor * (x ^ (deg - degDiv));
}
return new Polynomial[] { quotient, remainder };
}
}


class RSSoftDecoder : Decoder
{
int k;
int n;
FFE[] alphas;
FiniteField Fq = FFE.DEFAULT_FIELD;
private int nbMultiplicities;
public RSSoftDecoder(int k, FFE[] alphas, int nbMultiplicities)
{
if (alphas == null)
throw new ArgumentNullException("'alphas' cannot be null.");
if (!(0 < k & k < alphas.Length & alphas.Length <= Fq.q))
throw new ArgumentException("Arguments 'k' and 'n' do not satisfy: 0 < k < n <= q, where 'q' is the size of the field.");
for (int i = 0; i < n; i++)
for (int j = i+1; j < n; j++)
if(alphas[i] == alphas[j])
throw new ArgumentException("Evaluating locations must all be distinct");
this.k = k;
this.n = alphas.Length;
this.alphas = alphas;
this.nbMultiplicities = nbMultiplicities;
}
public FFE[] Decode(double[,] RM)
{
if (RM == null)
throw new ArgumentNullException("RM cannot be null");
if (RM.GetLength(0) != n | RM.GetLength(1) != Fq.q)
throw new ArgumentException("RM matrix should have size n x q (" + n + " x " + Fq.q + ").");
int[,] M = GreedyMAA(RM);
int omega = ComputeOmega(Cost(M));
List<Polynomial> polyList = ListDecode(M, omega);
if (polyList.Count == 0)
{
//throw new Exception("Decoding failed");
return new FFE[n];
}
double maxProba = 0;
Polynomial best = polyList[0];
foreach (Polynomial poly in polyList)
{
double proba = 1;
for (int i = 0; i < n; i++)
for (int j = 0; j < Fq.q; j++)
if (poly.Evaluate(alphas[i], Fq[0]) == Fq[j])
proba *= RM[i, j];
if (proba > maxProba)
{
maxProba = proba;
best = poly;
}
}
#if !MUTE
Console.WriteLine("\n--== RESULT ==--\n" + best);
#endif
FFE[] x = new FFE[n];
for (int i = 0; i < n; i++)
x[i] = best.Evaluate(alphas[i], Fq[0]);
return x;
}
public int[,] GreedyMAA(double[,] RM)
{
int[,] M = new int[n, Fq.q];
for (int m = 0; m < nbMultiplicities; m++)
{
double heighest = 0;
int hi = 0;
int hj = 0;
for (int i = 0; i < n; i++)
for (int j = 0; j < Fq.q; j++)
if (RM[i, j]/(M[i,j] + 1) > heighest)
{
heighest = RM[i, j]/(M[i,j] + 1);
hi = i;
hj = j;
}
M[hi, hj]++;
}
return M;
}
/*
public FFE[] Decode(FFE[] y)
{
if (y == null)
throw new ArgumentNullException("'y' cannot be null");
int[,] M = new int[n, Fq.q];
for (int i = 0; i < n; i++)
for (int j = 0; j < Fq.q; j++)
if(y[i] == Fq[j])
M[i, j] = nbMultiplicities / n; //nbMultiplicities must be a multiple of n!!!
int omega = ComputeOmega(n * nbMultiplicities * (nbMultiplicities + 1) / 2);
List<Polynomial> polyList = ListDecode(M, omega);
Polynomial best = polyList[0];
int maxAgree = 0;
foreach (Polynomial poly in polyList)
140{
int agree = 0;
for (int i = 0; i < n; i++)
if (poly.Evaluate(alphas[i], Fq[0]) == y[i])
agree++;
if (agree > maxAgree)
{
maxAgree = agree;
best = poly;
}
}
#if !MUTE
Console.WriteLine("\n--== RESULT ==--\n" + best);
#endif
FFE[] x = new FFE[n];
for (int i = 0; i < n; i++)
x[i] = best.Evaluate(alphas[i], Fq[0]);
return x;
}
*/
public List<Polynomial> ListDecode(int[,] M, int omega)
{
Polynomial Q = FindQ(M, omega);
#if !MUTE
Console.WriteLine("\n--== Interpolation finished ==--\n" + Q+"\n");
#endif
List<Polynomial> polyList = new List<Polynomial>();
FactorizeRR(Q, 0, new Polynomial(0,0), polyList);
#if !MUTE
Console.WriteLine("\n--== Factorization finished ==--");
foreach (Polynomial poly in polyList)
Console.WriteLine(poly);
#endif
return polyList;
}
public static long C(int j, int n)
{
long ans = 1;
if (j > n / 2)
j = n-j;
for (int i = 0; i < j; i++)
ans *= (n - i);
ans /= Fact(j);
return ans;
}
static int Fact(int n)
{
if (n <= 1)
return 1;
else
return n * Fact(n - 1);
}
// divides by the greatest power of X possible and shrink the array as much as possible
Polynomial NormalizeX(Polynomial Q)
{
int xPowerDivisor = -1;
for (int i = 0; i <= Q.GetMaxDegX() & xPowerDivisor == -1; i++)
for (int j = 0; j <= Q.GetMaxDegY(); j++)
if (Q[i, j] != Fq[0])
{
xPowerDivisor = i;
break;
}
if (xPowerDivisor == -1) {
return new Polynomial(0,0);
}
int xDegreesInExcess = -1;
for (int i = Q.GetMaxDegX(); i > 0 & xDegreesInExcess == -1; i--)
for (int j = 0; j <= Q.GetMaxDegY(); j++)
if (Q[i, j] != Fq[0])
{
xDegreesInExcess = Q.GetMaxDegX() - i;
break;
}
if (xPowerDivisor > 0 | xDegreesInExcess > 0)
{
Polynomial divided = new Polynomial(Q.GetMaxDegX() - xPowerDivisor - xDegreesInExcess, Q.GetMaxDegY());
for (int i = 0; i <= divided.GetMaxDegX(); i++)
for (int j = 0; j <= divided.GetMaxDegY(); j++)
divided[i, j] = Q[i + xPowerDivisor, j];
return divided;
}
else
return Q;
}
public Polynomial Y_To_XY_Plus_Gamma(Polynomial Q, FFE gamma)
{
Polynomial result = new Polynomial(Q.GetMaxDegX() + Q.GetMaxDegY(), Q.GetMaxDegY());
for (int i = 0; i <= Q.GetMaxDegX(); i++)
for (int j = 0; j <= Q.GetMaxDegY(); j++)
for (int d = 0; d <= j; d++)
result[i + d, d] += Fq[(int) C(d, j) % Fq.p] * Q[i, j] * (gamma ^ (j - d));
return result;
}
Polynomial HasseDerivative(Polynomial Q, int dx, int dy)
{
if (dx == 0 & dy == 0)
return Q;
Polynomial DQ = new Polynomial(Math.Max(Q.GetMaxDegX() - dx,0), Math.Max(Q.GetMaxDegY() - dy,0));
for (int i = 0; i <= Q.GetMaxDegX() - dx; i++)
for (int j = 0; j <= Q.GetMaxDegY() - dy; j++)
{
int ii = i + dx;
int jj = j + dy;
long ci = C(dx, ii);
long cj = C(dy, jj);
DQ[i, j] = Fq[(int) (C(dx,ii) % Fq.p)] * Fq[(int) (C(dy,jj) % Fq.p)] * Q[ii, jj];
}
return DQ;
}
public int Cost(int[,] M)
{
int cost = 0;
for (int i = 0; i < M.GetLength(0); i++)
for (int j = 0; j < M.GetLength(1); j++)
cost += M[i, j] * (M[i, j] + 1) / 2;
return cost;
}
public int ComputeOmega(int cost)
{
// this is a lower bound on Omega(cost)
int omega = (int) Math.Sqrt(2 * (k - 1) * cost) - 1;
int L = omega / (k - 1);
// now, we find the least Omega such that NbMonoms(Omega) > cost
int nbMonoms = (L + 1) * (omega+1 - L * (k - 1) / 2);
while (nbMonoms <= cost)
{
omega++;
L = omega / (k - 1);
nbMonoms = (L + 1) * (omega+1 - L * (k - 1) / 2);
}
return omega;
}
public Polynomial FindQ(int[,] M, int omega)
{
int L = omega / (k - 1);
Polynomial[] Q = new Polynomial[L + 1];
int[] wdeg = new int[L + 1];
for (int l = 0; l <= L; l++)
{
Q[l] = new Polynomial(omega, L);
Q[l][0, l] = Fq[1];
wdeg[l] = l * (k - 1);
}
FFE[] lambdas = new FFE[L + 1];
int lowestL;
for (int i = 0; i < alphas.Length; i++)
for (int j = 0; j < Fq.q; j++)
if (M[i, j] != 0)
{
#if VERBOSE
Console.WriteLine("i,j: " + alphas[i] + "," + Fq[j]);
#endif
for (int u = 0; u < M[i, j]; u++)
for (int v = 0; v < M[i, j] - u; v++)
{
#if !MUTE
#if !VERBOSE
Console.Write(".");
#endif
#endif
lowestL = -1;
for (int l = 0; l <= L; l++)
{
Polynomial Duv = HasseDerivative(Q[l], u, v);
lambdas[l] = Duv.Evaluate(alphas[i], Fq[j]);
#if VERBOSE
Console.WriteLine("Q["+l+"](x,y) = " +Q[l] + "\n => D_" + u + "," + v + " = " + Duv + "\n => lambda (" + alphas[i] + "," + Fq[j] + ") = " + lambdas[l]);
#endif
if (lambdas[l] != Fq[0])
if (lowestL == -1 || wdeg[l] < wdeg[lowestL])
lowestL = l;
}
#if VERBOSE
Console.WriteLine("\n" + "Lowest l is: " + lowestL + "\n");
#endif
if (lowestL != -1)
{
for (int l = 0; l <= L; l++)
if (lambdas[l] != Fq[0] & l != lowestL)
Q[l] = Q[l] - (lambdas[l]/lambdas[lowestL]) * Q[lowestL];
Q[lowestL] = (Polynomial.GetX() - alphas[i]) * Q[lowestL];
wdeg[lowestL] += 1;
}
}
}
return Q[ArgMin(wdeg)];
}
static int ArgMin(int[] array)
{
int argMin = -1;
for (int i = 0; i < array.Length; i++)
if (argMin == -1 || array[i] < array[argMin])
argMin = i;
return argMin;
}
public void FactorizeRR(Polynomial Q, int i, Polynomial f, List<Polynomial> fList)
{
Q = NormalizeX(Q);
#if VERBOSE
Console.WriteLine("i: " + i + " - k: " + k);
#elif !MUTE
Console.Write(".");
#endif
for (int j = 0; j < Fq.q; j++)
{
FFE eval = Q.Evaluate(Fq[0], Fq[j]);
if(eval == Fq[0])
{
#if VERBOSE
Console.WriteLine(Q);
Console.WriteLine("Q(0," + Fq[j] + ") = " + Q.Evaluate(Fq[0], Fq[j]));
#endif
Polynomial otherF = f + Fq[j] * (Polynomial.GetX()^i);
if (i == k - 1)
fList.Add(otherF);
else
{
Polynomial nextQ = Y_To_XY_Plus_Gamma(Q, Fq[j]);
FactorizeRR(nextQ, i + 1, otherF, fList);
}
}
}
}
}
	
}
